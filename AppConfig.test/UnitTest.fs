module AppConfig.test

open NUnit.Framework
open CharlieERP

type TConfig =
    { mutable IpAddress: string
      mutable Port: int
      mutable ServerName: string
      mutable Timeout: int }
    static member Default =
        { IpAddress = "0.0.0.0"
          Port = 1900
          ServerName = "charlie"
          Timeout = 2000 }

[<TestFixture>]
type JsonFileTests() =
    [<Test>]
    member this.NormalConfig() =
        let expected =
            { TConfig.Default with
                  ServerName = "bravo" }

        let res =
            AppConfig.Build TConfig.Default (Some(@".\config.json")) None None

        match res with
        | Ok r -> Assert.That(r, Is.EqualTo(expected))
        | Error _ -> Assert.Fail()


    [<Test>]
    member this.PartialFileConfig() =
        let expected = { TConfig.Default with Port = 1600 }

        let res =
            AppConfig.Build
                TConfig.Default
                (Some(@".\partialConfig.json"))
                None
                None

        match res with
        | Ok r -> Assert.That(r, Is.EqualTo(expected))
        | Error e -> Assert.Fail()

    [<Test>]
    member this.InvalidJSONConfig() =
        let res =
            AppConfig.Build
                TConfig.Default
                (Some(@".\invalidJSONConfig.json"))
                None
                None

        match res with
        | Ok _ -> Assert.Fail()
        | Error _ -> Assert.Pass()

    [<Test>]
    member this.NonExistingFile() =
        let expected = "Specified config file not found."

        let res =
            AppConfig.Build
                TConfig.Default
                (Some(@".\blaBlaConfig.json"))
                None
                None

        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type ArgsTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 15 }

        let res =
            AppConfig.Build
                TConfig.Default
                None
                (Some [| "--Port=15"
                         "--bbb=bla-bla-bla" |])
                None

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithString() =
        let expected =
            { TConfig.Default with
                  IpAddress = "16.16.16.16" }

        let res =
            AppConfig.Build
                TConfig.Default
                None
                (Some [| "--IpAddress=16.16.16.16"
                         "--bbb=blaBlaBla" |])
                None

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1Y5"" to integer"

        let res =
            AppConfig.Build TConfig.Default None (Some [| "--Port=1Y5" |]) None

        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type EnvTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 1616 }

        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "1616")

        let res =
            AppConfig.Build TConfig.Default None None (Some "APP_CONF_")

        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithString() =
        let expected =
            { TConfig.Default with
                  IpAddress = "160.160.160.160" }

        System.Environment.SetEnvironmentVariable
            ("APP_CONF_IpAddress", "160.160.160.160")

        let res =
            AppConfig.Build TConfig.Default None None (Some "APP_CONF_")

        System.Environment.SetEnvironmentVariable("APP_CONF_IpAddress", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1Y5"" to integer"

        let res =
            AppConfig.Build TConfig.Default None (Some [| "--Port=1Y5" |]) None

        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type PriorityTests() =
    [<Test>]
    member this.AllEntries() =
        let expected =
            { TConfig.Default with
                  Port = 32168
                  IpAddress = "16.32.64.128"
                  ServerName = "bravo" }

        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "32168")

        System.Environment.SetEnvironmentVariable
            ("APP_CONF_IpAddress", "8.16.32.64")

        let res =
            AppConfig.Build
                TConfig.Default
                (Some(@".\config.json"))
                (Some [| "--IpAddress=16.32.64.128" |])
                (Some "APP_CONF_")

        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()
