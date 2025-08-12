<!-- 
Pull Request Template for IIS FTP Simple Authentication Provider

For GenAI/Copilot users: Use the prompts and examples in each section to provide comprehensive PR information. Remove sections that don't apply to your changes.
-->

## Purpose
<!-- 
GenAI Prompt: Describe the main goal of this PR in 1-2 sentences. Examples:
- "Fixes authentication bug affecting users with special characters in passwords"
- "Adds support for Argon2 password hashing algorithm"
- "Updates documentation for Windows Server 2022 installation"
-->



## Changes Made
<!-- 
GenAI Prompt: Provide a bullet-point list of the specific changes in this PR. Include modified files, new features, bug fixes, or documentation updates. Be specific about what was changed and why.

Example:
- Modified `src/Core/Crypto/PasswordHasher.cs` to add Argon2 support
- Updated `ftpauth.config.json` schema to include Argon2 settings
- Added unit tests in `tests/Core.Tests/Crypto/PasswordHasherTests.cs`
- Updated README.md with Argon2 configuration examples
-->

### 🔧 Code Changes
- 

### 📝 Documentation Changes
- 

### 🧪 Test Changes
- 

## Type of Change
<!-- Mark with 'x' the type(s) that apply -->
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update (changes to documentation only)
- [ ] Code refactoring (no functional changes)
- [ ] Performance improvement
- [ ] Security enhancement
- [ ] Dependencies update
- [ ] CI/CD improvement
- [ ] Other (specify): _______________

## Related Issues
<!-- 
GenAI Prompt: Reference any related GitHub issues using keywords like "Fixes #123", "Closes #456", or "Related to #789". This automatically links and closes issues when the PR is merged.
-->

Fixes # (issue number)
Related to # (issue number)

## Testing Instructions
<!-- 
GenAI Prompt: Provide step-by-step instructions for testing your changes. Include specific commands, configuration examples, and expected outcomes. This helps reviewers verify your changes work correctly.

Example for authentication changes:
1. Build the solution: `dotnet build --configuration Release`
2. Deploy to test IIS instance
3. Create test user: `ftpauth create-user -u "test.user" -p "Test@123!" -f users.json`
4. Test FTP connection: `ftp ftp://test.user:Test@123!@localhost`
5. Verify authentication succeeds and user can access files
-->

### Setup
```bash
# Commands to set up test environment
```

### Test Cases
1. **Test Case 1:** 
   - **Steps:** 
   - **Expected Result:** 

2. **Test Case 2:** 
   - **Steps:** 
   - **Expected Result:** 

### Verification Commands
```bash
# Commands to verify the changes work correctly
```

## Security Considerations
<!-- 
GenAI Prompt: Describe any security implications of your changes. Consider:
- Are you handling sensitive data (passwords, keys, user info)?
- Do changes affect authentication or authorization logic?
- Are there potential vulnerabilities introduced?
- Do you follow secure coding practices?

Example:
- Password handling uses constant-time comparison to prevent timing attacks
- New encryption keys are generated using cryptographically secure random number generator
- Input validation prevents injection attacks
-->

- [ ] No security implications
- [ ] Changes handle sensitive data securely
- [ ] Authentication/authorization logic reviewed
- [ ] Input validation implemented
- [ ] No hardcoded secrets or credentials
- [ ] Follows secure coding practices

**Security Details:**


## Performance Impact
<!-- 
GenAI Prompt: Assess the performance impact of your changes:
- Do changes affect authentication speed?
- Are there new database queries or file operations?
- Do changes impact memory usage?
- Have you measured performance before/after?
-->

- [ ] No performance impact
- [ ] Minimal performance impact (< 5% change)
- [ ] Moderate performance impact (documented and justified)
- [ ] Performance improvement

**Performance Details:**


## Backward Compatibility
<!-- 
GenAI Prompt: Explain how your changes affect existing installations:
- Will existing configurations continue to work?
- Are database schema changes backward compatible?
- Do changes require migration steps?
- Are there any breaking changes to APIs or configuration?
-->

- [ ] Fully backward compatible
- [ ] Backward compatible with minor migration required
- [ ] Breaking changes (major version bump required)

**Compatibility Details:**


## Configuration Changes
<!-- 
GenAI Prompt: Document any changes to configuration files, CLI arguments, or environment variables. Provide before/after examples.

Example:
### New Configuration Options
```json
{
  "Hashing": {
    "Algorithm": "Argon2", // New option: BCrypt, PBKDF2, Argon2
    "Argon2": {
      "MemorySize": 65536,
      "Iterations": 3,
      "Parallelism": 4
    }
  }
}
```
-->

### Configuration File Changes
- [ ] No configuration changes
- [ ] New optional configuration added
- [ ] Existing configuration modified
- [ ] Configuration migration required

**Configuration Details:**


## Code Quality Checklist
<!-- GenAI Prompt: Verify your code meets quality standards -->

### Code Review
- [ ] Code follows project style guidelines
- [ ] Complex code is commented and documented
- [ ] No commented-out code or debug statements
- [ ] Error handling is comprehensive
- [ ] Code is DRY (Don't Repeat Yourself)
- [ ] Variable and method names are descriptive

### Testing
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] Edge cases are tested
- [ ] Error conditions are tested
- [ ] Test coverage is adequate
- [ ] Manual testing completed

### Documentation
- [ ] Code changes are documented
- [ ] README updated if necessary
- [ ] API documentation updated
- [ ] Configuration examples provided
- [ ] Migration guide provided (if needed)

## Deployment Notes
<!-- 
GenAI Prompt: Provide any special instructions for deploying these changes:
- Are there specific deployment steps?
- Do changes require IIS restart?
- Are there dependencies to install?
- Any rollback considerations?
-->

### Deployment Steps
1. 
2. 
3. 

### Rollback Plan
<!-- Describe how to rollback if issues are found -->


## Screenshots/Examples
<!-- 
GenAI Prompt: If your changes affect user interfaces, configuration files, or output, provide screenshots or examples. This helps reviewers understand the visual impact of your changes.
-->

### Before
```
<!-- Previous behavior/output -->
```

### After
```
<!-- New behavior/output -->
```

## Additional Notes
<!-- 
GenAI Prompt: Include any additional context, assumptions, trade-offs, or future considerations that reviewers should know about. This might include:
- Why certain implementation approaches were chosen
- Known limitations or future improvements
- Dependencies on other PRs or external changes
-->


---

## For Reviewers
<!-- 
GenAI Prompt: Help reviewers by highlighting:
- Areas that need careful attention
- Specific things to test
- Known concerns or trade-offs
- Files that are most critical to review
-->

**Focus Areas for Review:**
- [ ] Core authentication logic changes
- [ ] Security-sensitive code
- [ ] Configuration handling
- [ ] Error handling and logging
- [ ] Performance-critical sections

**Testing Priority:**
- [ ] Authentication flow
- [ ] Configuration loading
- [ ] Error scenarios
- [ ] Performance benchmarks
- [ ] Backward compatibility

---

<!-- 
Thank you for contributing to IIS FTP Simple Authentication Provider! 
Your pull request will be reviewed by maintainers and community members.
-->