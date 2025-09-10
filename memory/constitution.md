# Reisinger IntelliApp V4 Constitution

## Core Principles

### I. MVVM-First Architecture (NON-NEGOTIABLE)
**Strict separation of concerns in all code:**
- **Views**: Pure UI markup (XAML), no business logic, no data manipulation
- **ViewModels**: All business logic, state management, commands, data binding
- **Models**: Data structures, DTOs, immutable where possible
- **Services**: External dependencies, APIs, storage, platform-specific code
- **Violation**: Any business logic in code-behind files

### II. Dependency Injection First
**Every new feature must be DI-ready:**
- All services must have interfaces (IService pattern)
- Constructor injection mandatory for ViewModels and Services
- Registration in `ServiceCollectionExtensions.cs` required
- No static dependencies or service location patterns
- Enables testability and loose coupling

### III. Spec-Driven Development (NON-NEGOTIABLE)
**No code changes without specification:**
- Every feature requires: `/specify` → `/plan` → `/tasks` → implementation → evidence
- All changes documented in `specs/<id>/` folder structure
- Acceptance criteria must be measurable and testable
- Evidence collection mandatory (screenshots, logs, performance metrics)
- Small, focused PRs with clear rollback plans

### IV. Performance Budgets
**Measurable performance constraints:**
- Initial page load time ≤ 1.5 seconds
- No memory leaks in device scanning operations
- Smooth 60fps animations during zoom/pan operations
- PDF rendering under 500ms for typical floor plans
- No UI thread blocking during network operations

### V. Cross-Platform Compatibility
**Consistent behavior across all platforms:**
- Android, iOS, Windows, macOS support mandatory
- Platform-specific code isolated in `Platforms/` folders
- Shared UI behavior through MAUI abstractions
- Device-specific features (WiFi scanning, PDF handling) abstracted through services

## Technical Standards

### Architecture Patterns
- **MVVM**: Strict separation with BaseViewModel inheritance
- **Command Pattern**: All user interactions through ICommand
- **Observer Pattern**: INotifyPropertyChanged for data binding
- **Repository Pattern**: Data access through service abstractions
- **Factory Pattern**: Complex object creation isolated

### Code Quality Requirements
- **Async/Await**: All I/O operations must be asynchronous
- **Exception Handling**: Comprehensive try/catch with meaningful error messages
- **Logging**: Structured logging for debugging and monitoring
- **Documentation**: XML comments on all public APIs
- **Naming**: PascalCase for public members, camelCase for private

### Testing Standards
- **Unit Tests**: All ViewModels and critical Services must be testable
- **Integration Tests**: API calls, storage operations, device scanning
- **UI Tests**: Critical user workflows (device placement, navigation)
- **Performance Tests**: Memory usage, load times, animation smoothness
- **Manual Test Checklist**: Required for all features in tasks.md

## Development Workflow

### Feature Development Process
1. **Specify**: Create `specs/<id>/spec.md` with user stories and acceptance criteria
2. **Plan**: Document architectural impact and file changes in `plan.md`
3. **Tasks**: Break down into 5-12 atomic steps in `tasks.md`
4. **Implement**: Follow tasks sequentially with status updates
5. **Evidence**: Collect screenshots, logs, performance data in `evidence.md`
6. **Review**: PR with spec link and all ACs checked

### Quality Gates
- **Code Review**: Architecture compliance, MVVM adherence, DI usage
- **Testing**: Unit test coverage, integration test pass, manual checklist complete
- **Performance**: All budgets met, no regressions
- **Documentation**: Spec complete, evidence collected, changelog updated

### Continuous Integration
- **Build**: `dotnet build` passes on all platforms
- **Test**: `dotnet test` passes all automated tests
- **Lint**: Code style and formatting standards enforced
- **Package**: Successful app packaging for all target platforms

## Security & Compliance

### Data Protection
- **No Sensitive Data Logging**: MAC addresses, IP addresses, device credentials
- **Secure Storage**: Use platform secure storage for sensitive data
- **Network Security**: HTTPS mandatory for all API calls
- **Permission Management**: Minimal required permissions, user consent

### Platform Compliance
- **Android**: Target API 21+, privacy policy compliance
- **iOS**: App Store guidelines, privacy manifest requirements
- **Windows**: Microsoft Store certification requirements
- **macOS**: App Store distribution compliance

## Governance

### Constitution Authority
- **Supersedes All Other Practices**: This constitution takes precedence over any other guidelines
- **Amendment Process**: Requires specification, plan, tasks, and evidence collection
- **Version Control**: All changes tracked with semantic versioning
- **Compliance Verification**: All PRs must demonstrate constitution compliance

### Development Guidelines
- **Copilot Instructions**: `.github/copilot-instructions.md` provides runtime guidance
- **Architecture Document**: `ARCHITECTURE.md` contains current system design
- **Baseline Spec**: `specs/000-baseline/spec.md` documents current system state
- **Scripts**: Automated tools in `scripts/` folder for workflow support

**Version**: 1.0.0 | **Ratified**: 2025-01-11 | **Last Amended**: 2025-01-11