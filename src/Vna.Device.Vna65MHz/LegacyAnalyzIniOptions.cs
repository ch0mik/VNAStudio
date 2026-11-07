using Microsoft.Extensions.Configuration;

namespace Vna.Device.Vna65MHz;

/// <summary>Typed view of the VNA 1–65 MHz profile settings in the legacy INI format.</summary>
public sealed class LegacyAnalyzIniOptions
{
    public LegacyProfileSection Profile { get; set; } = new();
    [ConfigurationKeyName("PORTSETTING")]
    public LegacyPortSettingSection PortSetting { get; set; } = new();
    public LegacyParameterSection Parameters { get; set; } = new();
    public LegacyCalibrationSection Calibration { get; set; } = new();
    [ConfigurationKeyName("BANDS_LIMIT")]
    public LegacyBandLimitSection BandLimits { get; set; } = new();
    [ConfigurationKeyName("SPECIAL_CAL")]
    public LegacySpecialCalibrationSection SpecialCalibration { get; set; } = new();
    [ConfigurationKeyName("GRAF_PROP")]
    public LegacyGraphPropertiesSection GraphProperties { get; set; } = new();
    [ConfigurationKeyName("MEMORY")]
    public LegacyGeneratorMemorySection GeneratorMemory { get; set; } = new();
}

public sealed class LegacyPortSettingSection
{
    [ConfigurationKeyName("Port_type")]
    public int PortType { get; set; } = 2;
    [ConfigurationKeyName("RS_address")]
    public int RsAddress { get; set; }
    [ConfigurationKeyName("VNA_type")]
    public int VnaType { get; set; }
    public int Svna { get; set; }
}

public sealed class LegacyGeneratorMemorySection
{
    public long F_M0 { get; set; }
    public long F_M1 { get; set; }
    public long F_M2 { get; set; }
    public long F_M3 { get; set; }
    public long F_M4 { get; set; }
    public long F_M5 { get; set; }
    public long F_M6 { get; set; }
    public long F_M7 { get; set; }
    public long F_M8 { get; set; }
    public long F_M9 { get; set; }
}

/// <summary>Optional scan range shortcuts found in the legacy binary's INI reader.</summary>
public sealed class LegacyBandLimitSection
{
    public long F_M160s { get; set; }
    public long F_M160e { get; set; }
    public long F_M80s { get; set; }
    public long F_M80e { get; set; }
    public long F_M60s { get; set; }
    public long F_M60e { get; set; }
    public long F_M40s { get; set; }
    public long F_M40e { get; set; }
    public long F_M30s { get; set; }
    public long F_M30e { get; set; }
    public long F_M20s { get; set; }
    public long F_M20e { get; set; }
    public long F_M17s { get; set; }
    public long F_M17e { get; set; }
    public long F_M15s { get; set; }
    public long F_M15e { get; set; }
    public long F_M12s { get; set; }
    public long F_M12e { get; set; }
    public long F_M10s { get; set; }
    public long F_M10e { get; set; }
    public long F_M6s { get; set; }
    public long F_M6e { get; set; }
    public long F_M2s { get; set; }
    public long F_M2e { get; set; }
    public long F_M70cs { get; set; }
    public long F_M70ce { get; set; }
}

public sealed class LegacyProfileSection
{
    public string Id { get; set; } = "iw3hev-65mhz";
    public string DisplayName { get; set; } = "VNA 1-65 MHz";
}

public sealed class LegacyParameterSection
{
    [ConfigurationKeyName("DDS_CLOCK")]
    public long DdsClock { get; set; }
    [ConfigurationKeyName("DDS_PLL")]
    public int DdsPll { get; set; } = 1;
    public long FMin { get; set; }
    public long FMax { get; set; }
    public int Champ { get; set; }
    [ConfigurationKeyName("START_PAUSE")]
    public int StartPause { get; set; } = 10;
    [ConfigurationKeyName("STEP_PAUSE")]
    public int StepPause { get; set; }
    public double InputVelFactor { get; set; } = 0.66;
}

public sealed class LegacyCalibrationSection
{
    [ConfigurationKeyName("RL_OFFSET_vna")]
    public double RlOffsetVna { get; set; }
    [ConfigurationKeyName("RL_OFFSET_filter")]
    public double RlOffsetFilter { get; set; }
}

public sealed class LegacySpecialCalibrationSection
{
    [ConfigurationKeyName("Z_value")]
    public double ZValue { get; set; } = 50;
    [ConfigurationKeyName("Adc_phres")]
    public double AdcPhres { get; set; }
    [ConfigurationKeyName("Adc_magres")]
    public double AdcMagres { get; set; }
    [ConfigurationKeyName("RL_a")]
    public double RlA { get; set; } = 1;
    [ConfigurationKeyName("RL_b")]
    public double RlB { get; set; }
    [ConfigurationKeyName("PH_a")]
    public double PhA { get; set; } = 1;
    [ConfigurationKeyName("PH_b")]
    public double PhB { get; set; }
}

public sealed class LegacyGraphPropertiesSection
{
    public long ColorS1 { get; set; } = 165335;
    public long ColorS2 { get; set; } = 16711680;
    public long ColorS3 { get; set; } = 32768;
    public long ColorS4 { get; set; } = 65305;
    public long ColorS5 { get; set; } = 16521960;
    public long ColorS6 { get; set; } = 255;
    public long BackGround { get; set; } = 15790320;
}
