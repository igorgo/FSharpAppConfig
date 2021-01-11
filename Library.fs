namespace CharlieERP

open System
open System.IO
open System.Xml
open System.Runtime.Serialization
open System.Runtime.Serialization.Json
open Microsoft.FSharp.Reflection

[<DataContract>]
type TStrArray =
    { [<DataMember>]
      mutable value: string [] }

[<DataContract>]
type TIntArray =
    { [<DataMember>]
      mutable value: int [] }

type AppConfigBuilder<'T>(?defaultConfig: 'T) =
    let mutable conf =
        match defaultConfig with
        | Some c -> Ok c
        | None -> Error("Neither default config nor file specified")

    let fields = FSharpType.GetRecordFields(typeof<'T>)

    let genStream (s: string) =
        let ms = new MemoryStream()
        let writer = new StreamWriter(ms)
        writer.Write(s)
        writer.Flush()
        ms.Position <- int64 0
        ms

    let setValue (key: Reflection.PropertyInfo, value: string) =
        match conf with
        | Ok c ->
            match (key.PropertyType) with
            | t when t = typeof<bool> ->
                match value with
                | "true" -> key.SetValue(c, true)
                | "false" -> key.SetValue(c, false)
                | _ -> conf <- Error(sprintf "Couldn't convert \"%s\" to boolean" value)
            | t when t = typeof<String> -> key.SetValue(c, value)
            | t when t = typeof<int> ->
                match Int32.TryParse(value) with
                | (true, i) -> key.SetValue(c, i)
                | _ -> conf <- Error(sprintf "Couldn't convert \"%s\" to integer" value)
            | t when t = typeof<int []> ->
                let arr =
                    genStream (sprintf "{\"value\":%s}" value)
                    |> DataContractJsonSerializer(typeof<TIntArray>)
                        .ReadObject :?> TIntArray

                key.SetValue(c, arr.value)
            | t when t = typeof<string []> ->
                let arr =
                    genStream (sprintf "{\"value\":%s}" value)
                    |> DataContractJsonSerializer(typeof<TStrArray>)
                        .ReadObject :?> TStrArray

                key.SetValue(c, arr.value)
            | _ -> conf <- Error(sprintf "Wrong type of \"%s\" property" key.Name)
        | Error _ -> ()

    member this.Config = conf

    member this.ReadFromFile fileName =
        match File.Exists(fileName) with
        | true ->
            use fs = new FileStream(fileName, FileMode.Open)

            try
                match Path.GetExtension(fileName) with
                | ".xml" ->
                    let reader =
                        XmlDictionaryReader.CreateTextReader(fs, XmlDictionaryReaderQuotas())

                    let ser = DataContractSerializer(typeof<'T>)
                    let fConf = ser.ReadObject reader :?> 'T
                    conf <- Ok fConf
                | ".json" ->
                    let ser = DataContractJsonSerializer(typeof<'T>)
                    fs.Position <- int64 0
                    let fConf = ser.ReadObject fs :?> 'T
                    conf <- Ok fConf
                | _ -> conf <- Error "Unknown file type"
            with exn -> conf <- Error exn.Message
        | false -> conf <- Error "Specified config file not found."

        this

    member this.ApplyEnv(prefix: string) =
        match conf with
        | Ok _ ->
            for key in fields do
                match Environment.GetEnvironmentVariable(prefix + key.Name) with
                | null -> ()
                | v -> setValue (key, v)
        | Error _ -> ()

        this

    member this.ApplyArgs(args: string []) =
        match conf with
        | Ok _ ->
            let argsDict =
                Collections.Generic.Dictionary<string, string>()

            for arg in args do
                let m =
                    System.Text.RegularExpressions.Regex.Match(arg, @"--([a-zA-Z]+)=(.+)$")

                if m.Success
                then argsDict.Add(m.Groups.[1].Value, m.Groups.[2].Value)

            for key in fields do
                if argsDict.ContainsKey(key.Name) then setValue (key, argsDict.Item(key.Name))
        | Error _ -> ()

        this
