## General instructions

At the end of a task, if any code changes have been done, run
```bash
dotnet build Backend && dotnet test Backend
```
If tests fail because mistake in tests, fix the tests.
Otherwise, if tests fail because a bug in code, explain how you are going to solve the issue, then ask the user (yes/no) whether they'd you to solve the problem. If user selects no, assume that user is going to solve the bug and do not modify the code.

After tests have run successfully, run
```bash
dotnet format Backend
```