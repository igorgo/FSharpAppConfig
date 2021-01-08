namespace CharlieERP

open System
open System.IO
open System.Text.Json
open Microsoft.FSharp.Reflection

module AppConfig =

    let readFromFile<'T> (fileName: string): Result<'T, string> =
        match File.Exists(fileName) with
        | true ->
            let fileContent = File.ReadAllText(fileName)

            try
                let fConf =
                    JsonSerializer.Deserialize<'T> fileContent

                Ok fConf
            with
            | :? ArgumentNullException -> Error "Config file is empty"
            | ex -> Error ex.Message
        | false -> Error "Specified config file not found."


    let Build (defaultConfig: 'T)
              (fileName: string option)
              (args: Option<string []>)
              (envPrefix: string option)
              : Result<'T, string> =
        let fields = FSharpType.GetRecordFields(typeof<'T>)
        let mutable conf = defaultConfig
        let mutable err: string list = []

        match fileName with
        | Some (s) ->
            match readFromFile<'T> s with
            | Ok r ->
                printfn "r: %O" r

                for key in fields do
                    let value = key.GetValue r

                    match value with
                    | null -> ()
                    | v -> key.SetValue(conf, v)

                ()
            | Error ex ->
                err <- ex :: err
                ()
        | None -> ()

        let pref =
            match envPrefix with
            | Some p -> p
            | None -> ""

        let setIntVal (key: Reflection.PropertyInfo, s: string) =
            match Int32.TryParse(s) with
            | (true, i) -> key.SetValue(conf, i)
            | _ ->
                err <-
                    sprintf "Couldn't convert \"%s\" to integer" s
                    :: err

        for key in fields do
            let value =
                Environment.GetEnvironmentVariable(pref + key.Name)

            match value with
            | null -> ()
            | v ->
                match (key.PropertyType) with
                | t when t = typeof<String> -> key.SetValue(conf, v)
                | t when t = typeof<Int32> -> setIntVal (key, v)
                | _ ->
                    err <-
                        sprintf "Wrong type of \"%s\" property" key.Name
                        :: err

        match args with
        | Some aConf ->
            let argsDict =
                Collections.Generic.Dictionary<string, string>()

            for arg in aConf do
                let m =
                    System.Text.RegularExpressions.Regex.Match
                        (arg, @"--([a-zA-Z]+)=(.+)$")

                if m.Success
                then argsDict.Add(m.Groups.[1].Value, m.Groups.[2].Value)

            for key in fields do
                if argsDict.ContainsKey(key.Name) then
                    let v = argsDict.Item(key.Name)

                    match (key.PropertyType) with
                    | t when t = typeof<String> -> key.SetValue(conf, v)
                    | t when t = typeof<Int32> -> setIntVal (key, v)
                    | _ ->
                        err <-
                            sprintf "Wrong type of \"%s\" property" key.Name
                            :: err

            ()
        | None -> ()

        match err with
        | [] -> Ok conf
        | messages -> Error(messages |> String.concat Environment.NewLine)
