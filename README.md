# Auto-Grading Solution

A local Windows-based auto-grading system for student programming assignments. This system grades student projects based on test kits that define expected behavior through Excel files.

## Features

- **Excel-Based Test Kits**: Define test suites and test cases using Excel files (header.xlsx, environment.xlsx, detail.xlsx)
- **Process Management**: Automatically starts, monitors, and captures output from client/server processes
- **Flexible Validation**: Validates client console, server console, and network traffic (HTTP/TCP)
- **Database Reset**: Automatically resets databases by injecting ALTER DROP blocks if needed
- **Partial Test Kits**: Missing sheets or columns in test kits are ignored (intentional design)
- **Detailed Reports**: Generates comprehensive logs and Excel reports with expected vs actual data
- **Configurable Grading**: Support for different grading modes (client-only, server-only, console-only, etc.)
- **Staged Execution**: Executes test cases in stages with proper process lifecycle management

## Architecture

```
Application/
  Client/               - CLI application (entry point)
  GraderCore/           - Core grading engine library
    Models/             - Domain models
    Services/           - Business logic services
    Abstractions/       - Interfaces
    Keywords/           - Constants and configuration keywords
```

## Test Kit Structure

A test suite consists of:

```
TestSuite/
  header.xlsx           - Test case list with marks
  environment.xlsx      - Environment configuration (ports, database, etc.)
  TC01_TestName/
    header.xlsx         - Test case configuration
    detail.xlsx         - Test stages and expected data
  TC02_AnotherTest/
    header.xlsx
    detail.xlsx
  ...
  Meta/
    database.sql        - Database initialization script
    Given/              - Standard client/server executables (optional)
```

### Suite header.xlsx

Contains test case marks in the "QuestionMark" sheet:

| Cases | Mark |
|-------|------|
| TC01_Start | 1 |
| TC02_SendData | 5 |
| TC03_ServerError | 2 |

### Suite environment.xlsx

Contains environment configuration in the "Config" sheet:

| Key | Value |
|-----|-------|
| Environment_Type | dotnet |
| Code_Container_Internal_Port | 8000 |
| Code_Container_Host_Port | 8001 |
| Database_Username | sa |
| Database_Password | sa |
| Default_Database_Name | MyDatabase |
| Default_Database_File_Path | Meta\database.sql |

### Test Case header.xlsx

Contains test case properties in the "Testcase_Property" sheet:

| Key | Value |
|-----|-------|
| Test Case ID | TC01 |
| Timeout(Seconds) | 120 |
| Domain | http://localhost:5235 |
| Grade_Content | Client |

**Grade_Content** determines what to grade:
- `Client` - Only client is graded (use standard server)
- `Server` - Only server is graded (use standard client)
- `Both` - Both client and server are graded

### Test Case detail.xlsx

Contains multiple sheets:

#### User Sheet
Defines the sequence of actions:

| Stage | Input | Action |
|-------|-------|--------|
| 1 | | StartClient |
| 2 | | StartServer |
| 3 | 1 | Input |
| 4 | | CloseClient |

**Actions**: `StartClient`, `StartServer`, `Input`, `CloseClient`, `CloseServer`

#### Client Sheet (optional)
Expected client console output:

| Stage | Console |
|-------|---------|
| 1 | Client application started.\nPlease enter number: |
| 3 | Result: 123 |

#### Server Sheet (optional)
Expected server console output:

| Stage | Console |
|-------|---------|
| 2 | Server started.\n |
| 3 | GET /api/data/1 |

#### Network Sheet (optional)
Expected network traffic:

| Stage | Url | REQ_Payload | RES_Payload |
|-------|-----|-------------|-------------|
| 3 | http://localhost:8000/api/data/1 | | {"id": 1, "value": "test"} |

**Note**: Sheets and columns can be omitted. Missing data means "don't validate this aspect" - this is intentional design, not an error.

## Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server (if database operations are needed)
- Windows OS

### Build

```bash
cd Auto-Grading-Solution
dotnet build Application/Client/Client.csproj
```

The executable will be at: `Application/Client/bin/Debug/net8.0/Client.exe`

## Usage

### Basic Command

```bash
Client ExecuteSuite --suite <path> --out <path> [options]
```

### Required Arguments

- `--suite <path>`: Path to test suite folder (contains header.xlsx and environment.xlsx)
- `--out <path>`: Path to output folder where results will be saved

### Optional Arguments

- `--client <path>`: Path to student's client executable (exe or dll)
- `--server <path>`: Path to student's server executable (exe or dll)
- `--client-appsettings <path>`: Path to client appsettings.json template
- `--server-appsettings <path>`: Path to server appsettings.json template
- `--db-script <path>`: Path to database script (overrides test kit default)
- `--timeout <seconds>`: Stage timeout in seconds (default: 30)
- `--grading-mode <mode>`: Grading mode (default, client, server, console, http)

### Grading Modes

- `default`: Validate all aspects (client console, server console, network traffic)
- `client`: Validate only client-side output
- `server`: Validate only server-side output
- `console`: Validate only console outputs (no network validation)
- `http`: Validate only network traffic (no console validation)

### Examples

#### Grade both client and server
```bash
Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results \
  --client ./student/Client.exe --server ./student/Server.exe
```

#### Grade client only (using standard server from test kit)
```bash
Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results \
  --client ./student/Client.exe --grading-mode client
```

#### Grade server only (using standard client from test kit)
```bash
Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results \
  --server ./student/Server.exe --grading-mode server
```

#### Grade with custom timeout
```bash
Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results \
  --client ./student/Client.exe --timeout 60
```

### Meta/Given Folder

The `Meta/Given/` folder in each test kit should contain reference/standard client and server executables:

```
TestSuite/Meta/Given/
  Client.exe    - Reference client executable
  Server.exe    - Reference server executable
```

**Purpose**: When `Grade_Content` in test case header.xlsx specifies:
- `"Client"` - Grade student's client using standard server from Meta/Given
- `"Server"` - Grade student's server using standard client from Meta/Given  
- `"Both"` - Grade both student's client and server

**Status**: ⚠️ The Meta/Given folders exist but are currently empty. To test the grading system with a happy case, copy reference executables to `Meta/Given/` before running. See `Meta/Given/README.md` in each test kit for details.

## Output

Results are saved in a timestamped folder: `GradeResult_YYYYMMDD_HHMMSS/`

### Generated Files

1. **GradeProcess.log**: Detailed log of the grading process including:
   - Test case execution steps
   - Process start/stop events
   - Validation results
   - Errors and warnings

2. **GradeResults.xlsx**: Excel file with:
   - **Summary** sheet: Overall results for each test case
   - **TC01_**, **TC02_**, etc. sheets: Detailed stage-by-stage results showing expected vs actual data

3. **Summary.txt**: Plain text summary of results

### Example GradeResults.xlsx Structure

**Summary Sheet:**

| Test Case | Max Marks | Earned Marks | Passed | Summary |
|-----------|-----------|--------------|--------|---------|
| TC01_Start | 1.0 | 1.0 | PASS | Passed 2/2 stages |
| TC02_SendData | 5.0 | 2.5 | FAIL | Passed 2/4 stages |
| TOTAL | 6.0 | 3.5 | 58.33% | |

**TC01_Start Sheet:**

| Stage | Action | Passed | Expected Client | Actual Client | Expected Server | Actual Server | Validation Messages |
|-------|--------|--------|-----------------|---------------|-----------------|---------------|---------------------|
| 1 | StartClient | PASS | Client started... | Client started... | | | |
| 2 | StartServer | PASS | | | Server started... | Server started... | |

## How It Works

### Execution Flow

1. **Load Suite**: Read header.xlsx and environment.xlsx from suite folder
2. **For Each Test Case**:
   a. Load test case configuration from header.xlsx
   b. Parse test stages from detail.xlsx
   c. Reset database if needed
   d. Execute each stage:
      - Perform action (StartClient, StartServer, Input, etc.)
      - Wait for processes to generate output
      - Capture console output from processes
      - Capture network traffic (if middleware active)
   e. Validate captured data against expected data
   f. Calculate marks based on passed/failed stages
3. **Generate Reports**: Create logs and Excel files

### Process Management

- Client and server processes are started in separate processes
- Console output (stdout and stderr) is captured asynchronously
- Input can be sent to processes via stdin
- Processes are cleanly terminated after test execution
- Middleware is started when both client and server are running

### Database Reset

1. Read SQL script file
2. Check if ALTER DROP block exists
3. If missing, inject block at beginning:
   ```sql
   USE master;
   GO
   ALTER DATABASE [DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   GO
   DROP DATABASE IF EXISTS [DatabaseName];
   GO
   ```
4. Execute script in batches (split on GO statements)

### Data Validation

- **Console Output**: Text comparison with normalization (whitespace, line endings)
- **JSON Data**: Semantic JSON comparison using JToken.DeepEquals
- **Missing Expectations**: If expected data is null/empty, validation is skipped
- **Partial Matches**: Marks awarded proportionally to passed stages

## Configuration

### Timeouts

Timeouts can be configured at multiple levels:

1. **Global**: Via --timeout command-line argument
2. **Test Case**: Via Timeout(Seconds) in test case header.xlsx
3. **Default**: 30 seconds per stage

### Grading Configuration

The `GradingConfig` class controls what aspects are validated:

- `ValidateClientConsole`: Enable/disable client console validation
- `ValidateServerConsole`: Enable/disable server console validation
- `ValidateNetworkTraffic`: Enable/disable network validation
- `StageTimeoutSeconds`: Timeout for each stage
- `ProcessStartTimeoutSeconds`: Timeout for process startup

## Extensibility

### Adding New Actions

To add new user actions:

1. Add constant to `ProcessKeywords.cs`
2. Add case in `TestExecutor.ExecuteStage()`
3. Implement action handler method

### Adding New Validation Types

To add new validation types:

1. Add expected data fields to `TestStage` model
2. Add parsing logic to `TestCaseParser`
3. Add validation logic to `TestExecutor.ValidateStage()`
4. Add result fields to `StageResult`
5. Update report generation in `LoggingService`

### Custom Comparison Logic

Implement `IComparisonService` interface for custom comparison logic.

## Limitations and Known Issues

1. **Network Traffic Capture**: Currently stubbed - middleware proxy not yet implemented
   - Network validation will be skipped
   - Can be implemented using proxy or packet capture

2. **Database Operations**: Requires SQL Server
   - Connection must be available
   - Credentials must be correct

3. **Windows Only**: Designed for local Windows execution
   - Process management uses Windows-specific APIs
   - Paths use Windows conventions

4. **No Docker Support**: Unlike the original test-grader, this runs locally
   - No container isolation
   - Processes run on host machine

5. **Testing Status**: ⚠️ Not yet tested with actual client/server executables
   - Test kit parsing: ✅ Verified
   - Report generation: ✅ Verified
   - Process management: ✅ Implemented but not tested end-to-end
   - **Happy case testing**: ❌ Requires reference executables in Meta/Given folders
   - To test: Copy working client/server executables to `Meta/Given/` folders in test kits

## Development

### Project Structure

- **Models**: Data structures representing test suites, test cases, and results
- **Services**: Business logic for loading, parsing, executing, and reporting
- **Keywords**: Constants for Excel sheets, columns, actions, etc.
- **Abstractions**: Interfaces for dependency injection and testing

### Key Services

- `SuiteLoader`: Loads test suite configuration
- `TestCaseParser`: Parses test case details
- `ProcessManager`: Manages process lifecycle
- `TestExecutor`: Executes test stages and validates results
- `SuiteRunner`: Orchestrates entire suite execution
- `LoggingService`: Generates logs and reports
- `ComparisonService`: Compares expected vs actual data
- `DatabaseService`: Handles database operations
- `MiddlewareService`: Captures network traffic (stub)

## Troubleshooting

### Common Issues

**Build Errors**
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to restore packages

**Database Connection Failures**
- Check SQL Server is running
- Verify connection string
- Ensure database credentials are correct
- Check firewall settings

**Process Start Failures**
- Verify executable paths are correct
- Check file permissions
- Ensure .NET runtime is available for the executable
- Look for critical errors in GradeProcess.log

**Timeout Issues**
- Increase timeout using --timeout argument
- Check process isn't waiting for input
- Verify processes aren't hanging

**Missing Test Results**
- Check test case folders exist
- Verify Excel files are present and not corrupted
- Look for parsing errors in GradeProcess.log

## Contributing

When extending this system:

1. Follow existing code patterns and naming conventions
2. Add XML documentation comments to public APIs
3. Use Keywords classes for constants
4. Log important events using ILoggingService
5. Handle errors gracefully with try-catch
6. Update this README with new features

## References

- Reference Implementation: [auto-grading](https://github.com/NguyenHuuCuongK18/auto-grading.git)
- Original Docker-based System: [test-grader](https://github.com/NguyenHuuCuongK18/test-grader.git)

## License

This project is intended for educational use.
