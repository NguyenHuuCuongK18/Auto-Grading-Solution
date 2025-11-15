#!/bin/bash

# Auto-Grading Happy Case Test Script
# Tests the grading system with reference executables

set -e

echo "========================================="
echo "  AUTO-GRADING HAPPY CASE TEST"
echo "========================================="

# Start MSSQL Docker container
echo ""
echo "Starting MSSQL Docker container..."
docker-compose up -d

# Wait for MSSQL to be ready
echo "Waiting for MSSQL to be ready..."
sleep 20

# Check MSSQL health
docker-compose ps

# Build the grader
echo ""
echo "Building grader..."
dotnet build Application/Client/Client.csproj

# Test with EXE files
echo ""
echo "========================================="
echo "  TEST 1: Grading with .exe files"
echo "========================================="
dotnet run --project Application/Client/Client.csproj -- ExecuteSuite \
  --suite ./SampleTestKitsWithData/Testkit_HTTP_1 \
  --out ./TestResults/exe_test \
  --client ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project12/Project12.exe \
  --server ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project11/Project11.exe

# Check for DLL files
if [ -f "./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project12/Project12.dll" ]; then
    echo ""
    echo "========================================="
    echo "  TEST 2: Grading with .dll files"
    echo "========================================="
    dotnet run --project Application/Client/Client.csproj -- ExecuteSuite \
      --suite ./SampleTestKitsWithData/Testkit_HTTP_1 \
      --out ./TestResults/dll_test \
      --client ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project12/Project12.dll \
      --server ./SampleTestKitsWithData/Testkit_HTTP_1/Meta/Given/Project11/Project11.dll
else
    echo ""
    echo "DLL files not found - skipping DLL test"
fi

# Show results
echo ""
echo "========================================="
echo "  TEST RESULTS"
echo "========================================="
if [ -d "./TestResults/exe_test" ]; then
    echo ""
    echo "EXE Test Results:"
    find ./TestResults/exe_test -name "Summary.txt" -exec cat {} \;
fi

if [ -d "./TestResults/dll_test" ]; then
    echo ""
    echo "DLL Test Results:"
    find ./TestResults/dll_test -name "Summary.txt" -exec cat {} \;
fi

echo ""
echo "========================================="
echo "  Stopping MSSQL container..."
echo "========================================="
docker-compose down

echo ""
echo "Happy case tests completed!"
