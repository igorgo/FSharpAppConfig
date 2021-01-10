module AppConfig.test

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
      [<DataMember(IsRequired  = false) >]
      mutable Timeout: int
       }
with
    static member Default = {
          IpAddress = "0.0.0.0"
          Port = 1900
          ServerName = "charlie"
          Timeout = 2000 }

[<TestFixture>]
type XmlFileTests() =
    [<Test>]
    member this.NormalConfig() =
        let expected =
            { TConfig.Default with
                  ServerName = "bravo" }

        let res = AppConfigBuilder<TConfig>().ReadFromFile(@".\config.xml").Config
        match res with
        | Ok r -> Assert.That(r, Is.EqualTo(expected))
        | Error _ -> Assert.Fail()
    [<Test>]
    member this.InvalidJSONConfig() =
        let res = AppConfigBuilder<TConfig>().ReadFromFile(@".\invalidConfig.xml").Config
        match res with
        | Ok _ -> Assert.Fail()
        | Error _ -> Assert.Pass()
    [<Test>]
    member this.NonExistingFile() =
        let expected = "Specified config file not found."

        let res = AppConfigBuilder<TConfig>().ReadFromFile(@".\blaBlaConfig.xml").Config
        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)


[<TestFixture>]
type ArgsTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 15 }

        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyArgs([|
                         "--Port=15"
                         "--bbb=bla-bla-bla" |]).Config
        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithString() =
        let expected =
            { TConfig.Default with
                  IpAddress = "16.16.16.16" }
        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyArgs([|
                         "--IpAddress=16.16.16.16"
                         "--bbb=blaBlaBla" |]).Config
        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1Y5"" to integer"
        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyArgs([|
                         "--Port=1Y5" |]).Config
        match res with
        | Ok _ -> Assert.Fail()
        | Error ex -> Assert.AreEqual(expected, ex)

[<TestFixture>]
type EnvTests() =
    [<Test>]
    member this.WithInt() =
        let expected = { TConfig.Default with Port = 1616 }

        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "1616")

        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyEnv("APP_CONF_").Config
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

        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyEnv("APP_CONF_").Config
        System.Environment.SetEnvironmentVariable("APP_CONF_IpAddress", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()

    [<Test>]
    member this.WithInvalidInt() =
        let expected = @"Couldn't convert ""1G5"" to integer"
        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "1G5")
        let res = AppConfigBuilder<TConfig>(TConfig.Default).ApplyEnv("APP_CONF_").Config
        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "")
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
        let res = AppConfigBuilder<TConfig>()
                      .ReadFromFile(@".\config.xml")
                      .ApplyEnv("APP_CONF_")
                      .ApplyArgs([| "--IpAddress=16.32.64.128" |])
                      .Config
        System.Environment.SetEnvironmentVariable("APP_CONF_Port", "")
        System.Environment.SetEnvironmentVariable("APP_CONF_IpAddress", "")

        match res with
        | Ok r -> Assert.AreEqual(expected, r)
        | Error _ -> Assert.Fail()
