module AppConfig.test

open System
open NUnit.Framework
open CharlieERP
open System.Runtime.Serialization

  
[<DataContract(Name = "Config", Namespace = "")>]
type TConfig =
    { [<DataMember>]
      mutable IpAddress: string
      [<DataMember>]
      mutable Port: int
      [<DataMember>]
      mutable ServerName: string
      [<DataMember(IsRequired = false)>]
      mutable Timeout: int
      [<DataMember>]
      mutable LogLevels: string []
      [<DataMember>]
      mutable Years: int [] }
    static member Default =
        { IpAddress = "0.0.0.0"
          Port = 1900
          ServerName = "charlie"
          Timeout = 2000
          LogLevels = [| "error"; "info" |]
          Years = [| 2016; 1932; 1964 |] }

[<TestFixture>]
type XmlFileTests() =
    [<Test>]
    member this.NormalConfig() =
        let expected =
            { TConfig.Default with
                  ServerName = "bravo" }
        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"./config.xml")
                .Config

        match res with
        | Ok r -> Assert.That(r, Is.EqualTo(expected))
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.InvalidConfig() =
        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"./invalidConfig.xml")
                .Config

        match res with
        | Ok _ -> Assert.Fail()
        | Error _ -> Assert.Pass()

    [<Test>]
    member this.NonExistingFile() =
        let expected = "Specified config file not found."

        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"./blaBlaConfig.xml")
                .Config

        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type JsonFileTests() =
    [<Test>]
    member this.NormalConfig() =
        let expected =
            { TConfig.Default with
                  ServerName = "bravo" }
        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"config.json")
                .Config

        match res with
        | Ok r -> Assert.That(r, Is.EqualTo(expected))
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.InvalidConfig() =
        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"invalidConfig.xml")
                .Config

        match res with
        | Ok _ -> Assert.Fail()
        | Error _ -> Assert.Pass()

[<TestFixture>]
type ArgsTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 15 }

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyArgs([| "--Port=15"; "--bbb=bla-bla-bla" |])
                .Config

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithIntArray() =
        let expected = { TConfig.Default with Years = [|2020; 2019; 2021|] }

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyArgs([| "--Years=[2020,2019,2021]"; "--bbb=bla-bla-bla" |])
                .Config

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithString() =
        let expected =
            { TConfig.Default with
                  IpAddress = "16.16.16.16" }

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyArgs([| "--IpAddress=16.16.16.16"
                              "--bbb=blaBlaBla" |])
                .Config

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithStringArray() =
        let expected =
            { TConfig.Default with
                  LogLevels = [|"db"; "api"|] }

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyArgs([| "--LogLevels=[\"db\", \"api\"]"
                              "--bbb=blaBlaBla" |])
                .Config

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1Y5"" to integer"

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyArgs([| "--Port=1Y5" |])
                .Config

        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type EnvTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 1616 }

        Environment.SetEnvironmentVariable("APP_CONF_Port", "1616")

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyEnv("APP_CONF_")
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_Port", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithIntArray() =
        let expected = { TConfig.Default with Years = [|1989; 1999; 2006; 2009|] }

        Environment.SetEnvironmentVariable("APP_CONF_Years", "[1989, 1999, 2006, 2009]")

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyEnv("APP_CONF_")
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_Years", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithString() =
        let expected =
            { TConfig.Default with
                  IpAddress = "160.160.160.160" }

        Environment.SetEnvironmentVariable
            ("APP_CONF_IpAddress", "160.160.160.160")

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyEnv("APP_CONF_")
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_IpAddress", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithStringArray() =
        let expected =
            { TConfig.Default with
                  LogLevels = [|"sys"; "security"|] }

        Environment.SetEnvironmentVariable
            ("APP_CONF_LogLevels", "[\"sys\", \"security\"]")

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyEnv("APP_CONF_")
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_LogLevels", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1G5"" to integer"
        Environment.SetEnvironmentVariable("APP_CONF_Port", "1G5")

        let res =
            AppConfigBuilder<TConfig>(TConfig.Default)
                .ApplyEnv("APP_CONF_")
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_Port", "")

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

        Environment.SetEnvironmentVariable("APP_CONF_Port", "32168")

        Environment.SetEnvironmentVariable
            ("APP_CONF_IpAddress", "8.16.32.64")

        let res =
            AppConfigBuilder<TConfig>()
                .ReadFromFile(@"config.xml")
                .ApplyEnv("APP_CONF_")
                .ApplyArgs([| "--IpAddress=16.32.64.128" |])
                .Config

        Environment.SetEnvironmentVariable("APP_CONF_Port", "")
        Environment.SetEnvironmentVariable("APP_CONF_IpAddress", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()
