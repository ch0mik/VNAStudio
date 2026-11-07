using System.Reflection;
using System.Reflection.Emit;
using Vna.App.ViewModels;
using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class DevicePluginArchitectureTests
{
    private static readonly HashSet<string> ProtocolProfileMembers = new(StringComparer.Ordinal)
    {
        "DdsClockHz",
        "DdsPllPrescaler",
        "MagnitudeAdcDbPerCount",
        "PhaseAdcDegreesPerCount",
        "LegacyVnaType",
        "LegacySvna",
        "LegacyCalibration",
        "StartPauseMilliseconds",
        "StepPauseMilliseconds",
    };

    [Fact]
    public void AppAndCoreDoNotReadVna65ProtocolProfileFields()
    {
        var assemblyFindings = FindProtocolProfileMemberReferences(typeof(MainWindowViewModel).Assembly)
            .Concat(FindProtocolProfileMemberReferences(typeof(DeviceProfile).Assembly))
            .ToArray();

        Assert.True(
            assemblyFindings.Length == 0,
            "Application and Core operational code must not read VNA 1–65 MHz profile fields; " +
            "DeviceProfile itself is retained only as a legacy session snapshot. References found: " +
            string.Join(", ", assemblyFindings));
    }

    [Fact]
    public void PublicPluginContractsDoNotExposeVna65ProtocolProperties()
    {
        var contractTypes = new[]
        {
            typeof(DevicePluginApiVersion),
            typeof(DeviceProfileDescriptor),
            typeof(DevicePluginDescriptor),
            typeof(DeviceConnectionRequest),
            typeof(IMeasurementSource),
            typeof(IDevicePlugin),
        };
        var protocolProperties = contractTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(property => property.Name)
            .Where(ProtocolProfileMembers.Contains)
            .ToArray();

        Assert.Empty(protocolProperties);
    }

    [Fact]
    public void CoreDoesNotReferenceDeviceImplementationOrSerialTransportAssemblies()
    {
        var references = typeof(DeviceProfile).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is "Vna.Device.Vna65MHz" or "Vna.Devices.Transport")
            .ToArray();

        Assert.Empty(references);
    }

    private static IEnumerable<string> FindProtocolProfileMemberReferences(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            // The old record is a compatibility DTO for saved legacy sessions. Its generated
            // PrintMembers/Deconstruct methods necessarily touch the stored values; runtime
            // application and domain behavior must otherwise remain independent of them.
            if (type == typeof(DeviceProfile))
                continue;

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var methods = type.GetMethods(Flags).Cast<MethodBase>()
                .Concat(type.GetConstructors(Flags));
            foreach (var method in methods)
            {
                var body = method.GetMethodBody();
                var bytes = body?.GetILAsByteArray();
                if (bytes is null)
                    continue;

                foreach (var member in ReadReferencedMembers(method, bytes))
                {
                    var name = member.Name.StartsWith("get_", StringComparison.Ordinal)
                        ? member.Name[4..]
                        : member.Name;
                    if (ProtocolProfileMembers.Contains(name))
                        yield return $"{assembly.GetName().Name}:{type.FullName}.{method.Name}->{member.DeclaringType?.Name}.{name}";
                }
            }
        }
    }

    private static IEnumerable<MemberInfo> ReadReferencedMembers(MethodBase method, byte[] il)
    {
        var oneByteOpcodes = new OpCode[0x100];
        var twoByteOpcodes = new OpCode[0x100];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opcode)
                continue;
            var value = unchecked((ushort)opcode.Value);
            if (value < 0x100)
                oneByteOpcodes[value] = opcode;
            else if ((value & 0xff00) == 0xfe00)
                twoByteOpcodes[value & 0xff] = opcode;
        }

        var position = 0;
        while (position < il.Length)
        {
            var first = il[position++];
            var opcode = first == 0xfe
                ? twoByteOpcodes[il[position++]]
                : oneByteOpcodes[first];
            var operandStart = position;

            switch (opcode.OperandType)
            {
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineTok:
                {
                    var token = BitConverter.ToInt32(il, operandStart);
                    MemberInfo? member = null;
                    try
                    {
                        member = method.Module.ResolveMember(
                            token,
                            method.DeclaringType?.GetGenericArguments(),
                            method.IsGenericMethod ? method.GetGenericArguments() : null);
                    }
                    catch (ArgumentException)
                    {
                        // Some runtime-generated or unusual metadata tokens cannot be resolved here.
                    }
                    catch (BadImageFormatException)
                    {
                        // Ignore malformed/non-member tokens; ordinary member operands resolve above.
                    }

                    if (member is not null)
                        yield return member;
                    position += 4;
                    break;
                }
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    position += 1;
                    break;
                case OperandType.InlineVar:
                    position += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    position += 8;
                    break;
                case OperandType.InlineSwitch:
                    var branchCount = BitConverter.ToInt32(il, operandStart);
                    position += sizeof(int) + branchCount * sizeof(int);
                    break;
                default:
                    position += 4;
                    break;
            }

            if (position > il.Length)
                throw new InvalidOperationException($"Invalid IL operand length in {method.DeclaringType?.FullName}.{method.Name}.");
        }
    }
}
