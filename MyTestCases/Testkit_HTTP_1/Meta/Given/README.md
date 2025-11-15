# Meta/Given Folder

This folder should contain the reference/standard client and server executables that are used for grading.

## Purpose

When grading student submissions:
- If `Grade_Content` = "Client" → Use **student's client** + **standard server from here**
- If `Grade_Content` = "Server" → Use **standard client from here** + **student's server**
- If `Grade_Content` = "Both" → Use **student's client** + **student's server**

## Expected Structure

```
Meta/Given/
  Client.exe (or Client.dll)      - Standard/reference client executable
  Server.exe (or Server.dll)      - Standard/reference server executable
  <any dependencies>              - DLLs, config files, etc.
```

## How to Populate

1. Build the reference client/server project
2. Copy the output files (exe/dll and dependencies) to this folder
3. Ensure executables are compatible with the test kit expectations

## Testing Happy Case

To test the grading system with a happy case:

```bash
# Copy reference executables here
cp /path/to/reference/Client.exe Meta/Given/
cp /path/to/reference/Server.exe Meta/Given/

# Run grading with these executables
dotnet run --project Application/Client/Client.csproj -- ExecuteSuite \
  --suite ./MyTestCases/Testkit_HTTP_1 \
  --out ./Results \
  --client ./MyTestCases/Testkit_HTTP_1/Meta/Given/Client.exe \
  --server ./MyTestCases/Testkit_HTTP_1/Meta/Given/Server.exe
```

## Status

⚠️ **Currently empty** - Reference executables need to be added for testing.

The grading system has been tested with:
- ✅ Test kit parsing (Excel files)
- ✅ Report generation (logs and Excel)
- ✅ Stage execution logic
- ❌ **Not yet tested with actual executables** (requires files in this folder)
