# Houmao Windows Makefile
# Usage: make <target>
#
# Targets:
#   build       - Build the project (Debug)
#   build-release - Build the project (Release)
#   test        - Run tests
#   publish     - Publish single-file executable
#   clean       - Clean build artifacts
#   check       - Check project structure and dependencies
#   install     - Install dependencies and build
#   all         - Clean, build, test, and publish
#   run         - Build and run the application
#   help        - Show this help message

.PHONY: all build build-release test publish clean check install run help

# Default target
all: clean build test publish

# Build Debug
build:
	@echo "Building Debug..."
	powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Configuration Debug

# Build Release
build-release:
	@echo "Building Release..."
	powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Configuration Release

# Run tests
test:
	@echo "Running tests..."
	dotnet test tests/Houmao.Tests/Houmao.Tests.csproj --verbosity normal

# Publish single-file executable
publish:
	@echo "Publishing..."
	powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Configuration Release -SingleFile

# Publish self-contained
publish-self-contained:
	@echo "Publishing self-contained..."
	powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Configuration Release -SelfContained -SingleFile

# Clean build artifacts
clean:
	@echo "Cleaning..."
	dotnet clean src/Houmao/Houmao.csproj --verbosity quiet
	dotnet clean tests/Houmao.Tests/Houmao.Tests.csproj --verbosity quiet
	@if exist publish rmdir /s /q publish
	@if exist *.zip del /q *.zip

# Check project structure
check:
	@echo "Checking project..."
	powershell -ExecutionPolicy Bypass -File scripts/check.ps1

# Install dependencies and build
install: check build
	@echo "Installation complete!"

# Build and run
run: build
	@echo "Running Houmao..."
	dotnet run --project src/Houmao/Houmao.csproj

# Run in Release mode
run-release: build-release
	@echo "Running Houmao (Release)..."
	dotnet run --project src/Houmao/Houmao.csproj --configuration Release

# Format code
format:
	@echo "Formatting code..."
	dotnet format src/Houmao/Houmao.csproj

# Restore packages
restore:
	@echo "Restoring packages..."
	dotnet restore src/Houmao/Houmao.csproj
	dotnet restore tests/Houmao.Tests/Houmao.Tests.csproj

# Show help
help:
	@echo.
	@echo Houmao Windows Build System
	@echo ==========================
	@echo.
	@echo Available targets:
	@echo   make build              - Build Debug configuration
	@echo   make build-release      - Build Release configuration
	@echo   make test               - Run unit tests
	@echo   make publish            - Publish single-file executable
	@echo   make publish-self-contained - Publish self-contained executable
	@echo   make clean              - Clean build artifacts
	@echo   make check              - Check project structure
	@echo   make install            - Check and build
	@echo   make run                - Build and run (Debug)
	@echo   make run-release        - Build and run (Release)
	@echo   make format             - Format code
	@echo   make restore            - Restore NuGet packages
	@echo   make all                - Clean, build, test, and publish
	@echo   make help               - Show this help
	@echo.
