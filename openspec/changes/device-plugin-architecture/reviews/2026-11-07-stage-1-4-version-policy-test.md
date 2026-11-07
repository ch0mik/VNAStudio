# Stage 1.4 review — plugin API compatibility policy

## Result

**PASS.** The independent review confirmed that the tests match the documented major/minor policy and the catalog's actual behavior.

The unit cases cover matching major versions, older/equal/newer minor versions, incompatible majors, negative plugin minor, and invalid host minor. Catalog tests cover registration of an older-minor plugin and rejection of a newer-minor plugin with an identifying diagnostic. The EN and PL guides state the same policy and link to its implementation.

## Verification

- `dotnet test tests/Vna.Tests/Vna.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~DevicePluginApiVersionTests` — 10 passed, 0 failed.
- No physical COM port, original application, or WOB connection was used.
