F# AppConfig Builder
====================

Application config builder.

Builds config `Record` from specified JSON config file, environment variables and command-line arguments.

Values priority:
  - Command-line arguments (highest)
  - Environment variable
  - Value from JSON file 
  - Default value (lowest)

Usage
-----

See `AppConfig.test` project

Build
-----

```
$ dotnet build AppConfig.sln
```

Test
---

```
dotnet test
```

Credits
-------

* Igor Gorodetsky
