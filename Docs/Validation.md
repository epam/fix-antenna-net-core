# Validation
## Initialization
To initialize a validator, load a dictionary and create validators for the different rules you need to check.

```csharp
FixVersionContainer fixVersion = FixVersionContainerFactory.GetFixVersionContainer(FixVersion.Fixt11);
FixVersionContainer appVersion = FixVersionContainerFactory.GetFixVersionContainer(FixVersion.Fix50Sp2);
IValidatorFactory validatorFactory = ValidatorFactory.CreateFactory(fixVersion, appVersion);

IValidatorContainer validators = validatorFactory.CreateRequiredValidator();

validators.PutNewValidator(ValidatorType.MessageWelformed, validatorFactory.CreateValidator(ValidatorType.MessageWelformed));
validators.PutNewValidator(ValidatorType.FieldAllowed, validatorFactory.CreateValidator(ValidatorType.FieldAllowed));
validators.PutNewValidator(ValidatorType.RequiredFields, validatorFactory.CreateValidator(ValidatorType.RequiredFields));
validators.PutNewValidator(ValidatorType.FieldOrder, validatorFactory.CreateValidator(ValidatorType.FieldOrder));
validators.PutNewValidator(ValidatorType.Duplicate, validatorFactory.CreateValidator(ValidatorType.Duplicate));
validators.PutNewValidator(ValidatorType.FieldDefinition, validatorFactory.CreateValidator(ValidatorType.FieldDefinition));
validators.PutNewValidator(ValidatorType.Conditional, validatorFactory.CreateValidator(ValidatorType.Conditional));
validators.PutNewValidator(ValidatorType.Group, validatorFactory.CreateValidator(ValidatorType.Group));

IFixMessageValidator validator = new ValidationEngine(validators);
```

## Validation
To validate a message:

```csharp
// get Fix message
FixMessage msg = ...;
FixErrorContainer errors = validator.ValidateFixMessage(msg);

//print all errors
foreach (FixError error in errors.Errors)
{
	Console.WriteLine(error);
}

//print high error
Console.WriteLine(errors.IsPriorityError);
```
