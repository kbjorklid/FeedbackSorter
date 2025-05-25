Please write unit test and related code in accordance to the steps outlined below

# General instructions
- Use `NSubstitute` when mocks or stubs are needed.
- Use xUnit. Use xUnit's assertions (and not a library like fluent assertions).

## Test object builders

When unit tests need to use, for example, a value object, create a test object builder to help keep the tests devoid of unnecessary details.

For example, for a class like this:
```csharp
public readonly record class PersonName(string FirstName, string LastName);
```
The following test object builder should be generated:
```csharp
public class PersonNameBuilder {
  private string _firstName = "John";
  private string _lastName = "Doe";

  public PersonNameBuilder WithFirstName(string firstName)
  {
    _firstName = firstName;
    return this;
  }

  public PersonNameBuilder WithLastName(string lastName)
  {
    _lastName = lastName;
    return this;
  }

  public PersonName Build()
  {
    return new PersonName(_firstName, _lastName);
  }
}
```


# 1. Analyze the code to be tested.

User should have provided you pointers to what code should be reviewed. Go ahead and read these files.

# 2. Consider creating test object object builders

# 2.1 Check if builders already exist

For code that you think Test object builders should exist, look if these already exist. You can expect these files to be named `[class_under_test]Builder.cs`, for example if class under test is `PersonName`, then the test object builder should exist in a file called `PersonNameBuilder.cs`. It should reside in a test project.

# 2.2 Create missing test object builders

If a test object builder does not yet exist, create it.

# 3. Write tests

Write the tests, create new unit test files where necessary

# 4. Run tests

To ensure tests pass, run
```bash
dotnet test
```

If some tests fail, fix them or the code under test, and run tests again.