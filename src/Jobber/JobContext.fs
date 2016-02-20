namespace Jobber

open System

type IStorageLocation<'TKey> =
    /// Store the object in storage, and return the key
    abstract member Store : 'TKey option -> obj -> 'TKey
    abstract member Get : 'TKey -> obj
    abstract member GetAs<'T> : 'TKey -> 'T option
    abstract member Exists : 'TKey -> bool


type JobRuntime =
    {
        Logger : Logary.Logger
    }

type JobContext =
    {
        Runtime: JobRuntime
        Storage: IStorageLocation<string>
        /// The path of the request being served.  Generally this will be [Job1Key].[Job2Key] etc
        Path: string
        Key: string option
        Data: Map<string, obj>
        Result: obj
    }

type JobResult =
    | UnstartedJob of Async<JobContext option>
    | StartedJob of Async<JobContext option> * System.Threading.CancellationToken

type Job = JobContext -> Async<JobContext option>

open Logary

module Logging =
    let private createLine level message (context:JobContext) =
        Logary.LogLine.create message context.Data level [] context.Path None

    let private createLineWithException level ex message (context:JobContext) =
        Logary.LogLine.create message context.Data level [] context.Path (Some(ex))

    let log level context message =
        let line = createLine level message context
        context.Runtime.Logger.Log line

    let logEx level context ex message =
        let line = createLineWithException level ex message context
        context.Runtime.Logger.Log line

    let verbose = log LogLevel.Verbose
    let debug = log LogLevel.Debug
    let info = log LogLevel.Info
    let warn = log LogLevel.Warn
    let warnEx ctx ex = logEx LogLevel.Warn ctx ex
    let error = log LogLevel.Error
    let errorEx = logEx LogLevel.Error
    let fatal = log LogLevel.Fatal
    let fatalEx ctx ex = logEx LogLevel.Fatal ctx ex

module File =
    let fileJob key path : Job =
        fun ctx -> ( async {
            match System.IO.File.Exists path with
            | true ->
                try
                    Logging.debug ctx "Found file"
                    use reader = new System.IO.StreamReader(System.IO.File.OpenRead(path))
                    let! content = reader.ReadToEndAsync () |> Async.AwaitTask
                    Logging.info ctx "Successfully read file"
                    return { ctx with Data = ctx.Data.Add(key,content)} |> Some
                with
                    | ex ->
                        Logging.errorEx ctx ex "Could not read file"
                        return None
            | false ->
                Logging.info ctx "No file found"
                return None
        })

module Storage =
    let missing key : Job =
        fun ctx -> ( async {
            match ctx.Storage.Exists key with
            | true ->
                Logging.debug ctx (sprintf "Found data in storage with key %s" key)
                return ctx |> Some
            | false ->
                Logging.debug ctx (sprintf "Found data in storage with key %s" key)
                return None
        })

    let storeAs key : Job =
        fun ctx -> (async {
            try
                let key' = ctx.Storage.Store (Some key) ctx.Result
                Logging.info ctx (sprintf "Stored data with key %s" key')
                return { ctx with Key = key |> Some} |> Some
            with
                | ex ->
                    Logging.errorEx ctx ex "Could not store data"
                    return None
        })

    let store : Job =
        fun ctx -> (async {
            match ctx.Key with
            | Some(key) ->
                Logging.verbose ctx (sprintf "Storing data with key %s" key)
                return! storeAs key ctx
            | None ->
                Logging.verbose ctx "Storing data with new key"
                try
                    let key = ctx.Storage.Store None ctx.Result
                    Logging.info ctx (sprintf "Stored data with key %s" key)
                    return { ctx with Key = key |> Some} |> Some
                with
                    | ex ->
                        Logging.errorEx ctx ex "Could not store data"
                        return None
        })

//module Http =
//    let get url : Job =
//        fun ctx -> (async {
//            let! result = Nap.getAsync<'T> url
//
//        })
    // implement nap-style module here
    // let get url : Job
