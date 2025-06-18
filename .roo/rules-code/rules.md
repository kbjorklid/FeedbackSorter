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

## Read project structure and other information

Start each task by reading `DESIGN.md`.


## Misc
- This project uses the 'nullable' annotation (example: `string?`) to mark that something may be null.
-  Always validate inputs at public API boundaries (e.g., Application Service command handlers, Entity/VO constructors exposed to Application layer). Within the internal scope of a method where the compiler guarantees non-nullability, further checks may be redundant. Use ArgumentNullException.ThrowIfNull() or similar.
- Do not use primary constructors when data validation at construction time is needed. Use 'old-style' constructors in such cases.

## Value Objects
- Validate data input: prevent creation of invalid value objects
- Extract related data into value objects. For example, instead of having a 'BeginDate' and 'EndDate' in an entity, create a DateRange value object and add proper validation there.
- Consider `readonly record struct` for very simple, small value objects. For more complex value objects, or those frequently used as properties in classes, `record class` often provides a better balance of immutability, value-based equality, and ease of use without potential performance pitfalls associated with large structs or frequent boxing.

## Unit Tests
- Use `NSubstitute` when mocks or stubs are needed.
- Use xUnit. Use xUnit's assertions (and not a library like fluent assertions)

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

Idea being that if the test does not care about some of the compulsory values, the builder will provide sensible defaults.
Example, where last name is not important: 
```csharp
PersonName annie = new PersonNameBuilder().WithFirstName("Annie").Build();  
```

## Other instructions for GenAI/LLM
- When there is a `dotnet` cli command for doing something, use that instead of modifying code.
- Use `dotnet add package` command to add dependencies.
- Before adding a library not mentioned by the user, confirm that user wants to start using that library.