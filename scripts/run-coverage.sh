#!/bin/bash
# Run code coverage analysis

echo "Running code coverage analysis..."

# Clean previous coverage data
rm -rf docs/coverage/*

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./docs/coverage/raw

# Generate HTML report (requires reportgenerator tool)
if command -v reportgenerator &> /dev/null
then
    reportgenerator \
        -reports:"docs/coverage/raw/**/coverage.cobertura.xml" \
        -targetdir:"docs/coverage" \
        -reporttypes:"Html;TextSummary"
    
    echo ""
    echo "Coverage report generated at: docs/coverage/index.html"
    cat docs/coverage/Summary.txt
else
    echo ""
    echo "Install reportgenerator for HTML reports:"
    echo "dotnet tool install -g dotnet-reportgenerator-globaltool"
fi

echo ""
echo "Coverage analysis complete!"
