# Auto-Grading Solution - Modular Architecture

## Project Structure

This solution follows a modular architecture inspired by test-grader.git with separation of concerns across multiple projects:

```
AutoGradingSolution.sln
├── Application/
│   ├── Client/                    - CLI entry point
│   ├── GraderCore/                - Legacy monolithic core (being phased out)
│   ├── ProcessLauncher/           - Process execution abstraction (.exe and .dll support)
│   ├── LocalLog/                  - Centralized logging service
│   ├── LocalDatabase/             - Database operations (SQL Server reset)
│   ├── LocalGrade/                - Test case grading and comparison
│   ├── LocalGraderConfig/         - Configuration models and keywords
│   ├── NetworkingMiddleware/      - Network traffic capture (planned)
│   └── SingleStudentGrade/        - Student grading orchestration
```

## Architecture Principles

### 1. ProcessLauncher Pattern
All process execution goes through ProcessLauncher, which:
- Supports both .exe and .dll files
- Handles async output capture
- Manages process lifecycle
- Provides standardized error handling

### 2. Separation of Concerns
Each project has a single responsibility:
- **ProcessLauncher**: Process management
- **LocalLog**: Logging and reporting
- **LocalDatabase**: SQL Server operations
- **LocalGrade**: Grading logic and comparison
- **LocalGraderConfig**: Configuration and models
- **NetworkingMiddleware**: Network traffic capture
- **SingleStudentGrade**: Orchestration of grading workflow

### 3. Dependency Flow
```
Client
  └─> SingleStudentGrade
        ├─> ProcessLauncher
        ├─> LocalLog
        ├─> LocalDatabase
        ├─> LocalGrade
        ├─> LocalGraderConfig
        └─> NetworkingMiddleware
```

## Projects

### ProcessLauncher
**Purpose**: Abstracts process execution for both .exe and .dll files

**Key Classes**:
- `ProcessRunner`: Main process execution class

**Features**:
- Automatic .dll detection (uses `dotnet` runtime)
- Async stdout/stderr capture
- Long-running process support
- Input injection support

**Usage**:
```csharp
var (stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
    "student.dll", 
    "--args", 
    "/working/dir",
    timeout: 30000
);
```

### LocalLog
**Purpose**: Centralized logging for all grading operations

**Key Classes**:
- `Logger`: Main logging class

**Features**:
- Process logging to GradeProcess.log
- Summary generation
- Timestamped entries
- Multiple log levels (INFO, WARN, ERROR, DEBUG)

**Usage**:
```csharp
var logger = new Logger("/output/directory");
logger.LogProcess("Starting grading...");
logger.LogError("Failed to start client");
logger.SaveSummary("Total: 8/10 marks");
```

### LocalDatabase
**Purpose**: SQL Server database operations

**Key Classes**:
- `DatabaseService`: Database reset and initialization

**Features**:
- ALTER DROP block injection
- GO statement parsing
- Connection string handling
- Non-critical failure mode

**Usage**:
```csharp
var dbService = new DatabaseService(logger);
bool success = dbService.ResetDatabase(
    "database.sql",
    "Server=localhost;Database=MyDB;User=sa;Password=***;"
);
```

### LocalGrade
**Purpose**: Test case grading and result comparison

**Planned Classes**:
- `TestCaseGrader`: Executes individual test cases
- `ComparisonEngine`: Compares expected vs actual results

**Features** (planned):
- Stage-based execution
- Multiple comparison strategies
- Detailed diff generation

### LocalGraderConfig
**Purpose**: Configuration models and constants

**Contents**:
- Models/ - Data models (TestSuite, TestCase, GradingConfig, Results)
- Keywords/ - Excel constants (sheet names, column names, file paths)

**Key Models**:
- `TestSuite`: Top-level test suite structure
- `TestCase`: Individual test case
- `TestStage`: Execution stage within test case
- `GradingConfig`: Grading configuration
- `TestResults`: Results and scoring

### NetworkingMiddleware
**Purpose**: Network traffic capture and validation

**Status**: Planned (currently stub)

**Features** (planned):
- HTTP/HTTPS traffic capture
- TCP traffic capture
- Request/response validation
- Payload comparison

### SingleStudentGrade
**Purpose**: Orchestrates the entire grading workflow

**Planned Classes**:
- `GradingOrchestrator`: Main orchestration logic
- `SuiteLoader`: Loads test suite from Excel files
- `TestCaseParser`: Parses individual test cases

**Workflow**:
1. Load test suite configuration
2. For each test case:
   - Reset database
   - Execute stages (Start/Input/Close)
   - Capture outputs
   - Compare with expected
   - Calculate score
3. Generate reports

## Migration Path

### Current State (Phase 1) ✅
- [x] Created modular project structure
- [x] ProcessLauncher with .exe and .dll support
- [x] LocalLog for centralized logging
- [x] LocalDatabase for SQL Server operations
- [x] LocalGraderConfig with models and keywords
- [x] Solution file tying projects together
- [x] All projects build successfully

### Phase 2 (In Progress)
- [ ] Migrate comparison logic to LocalGrade
- [ ] Migrate test execution to LocalGrade
- [ ] Create SingleStudentGrade orchestration
- [ ] Implement NetworkingMiddleware
- [ ] Update Client to use modular architecture

### Phase 3 (Future)
- [ ] Phase out GraderCore (legacy)
- [ ] Add unit tests for each module
- [ ] Performance optimization
- [ ] Advanced middleware features

## Building

```bash
# Build entire solution
dotnet build AutoGradingSolution.sln

# Build specific project
dotnet build Application/ProcessLauncher/ProcessLauncher.csproj

# Run tests (when added)
dotnet test AutoGradingSolution.sln
```

## Testing

### With Docker MSSQL
```bash
# Start MSSQL container
docker compose up -d

# Run grading with DLL files
dotnet run --project Application/Client/Client.csproj -- ExecuteSuite \
  --suite ./SampleTestKitsWithData/Testkit_HTTP_1 \
  --out ./Results \
  --client ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project12/Project12.dll \
  --server ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project11/Project11.dll

# Stop MSSQL
docker compose down
```

### Automated Happy Case Test
```bash
./test-happy-case.sh
```

## Key Differences from test-grader.git

While inspired by test-grader.git's architecture, this solution differs in:

1. **Test Kit Format**: Uses Excel-based test kits instead of JSON
2. **Grading Target**: Grades generic client-server applications, not specific frameworks
3. **Process Model**: Supports both .exe and .dll execution
4. **Partial Validation**: Intentionally ignores missing sheets/columns in test kits
5. **Database Handling**: Non-critical failure mode for SQL Server operations

## Dependencies

### External Packages
- ClosedXML - Excel file reading
- Newtonsoft.Json - JSON processing
- Microsoft.Data.SqlClient - SQL Server operations

### Internal Dependencies
See each project's .csproj for specific internal dependencies.

## Contributing

When adding new features:
1. Determine which project the feature belongs to
2. Add necessary dependencies to that project's .csproj
3. Update this README with the new functionality
4. Add unit tests (when test infrastructure is ready)
5. Update Client if needed to expose the feature

## License

[Your License Here]
