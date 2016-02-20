namespace Jobber

open FSharp.Collections.ParallelSeq

module Jobs =
    let private mergeData mergeDatums =
        Map.fold(fun data key datum ->
            match Map.tryFind key data with
            | Some datum' -> data |> Map.add key (mergeDatums key (datum,datum'))
            | None -> data |> Map.add key datum
        )
    let private mergeData' = mergeData (fun _ x -> fst x)
    let private mergePath (path1:char seq) path2 = 
        seq {
            for charTuple in Seq.zip path1 path2 do
            yield
                match charTuple with
                | (a,b) when a = b -> a
                | (_,_) -> '_'
        }
        |> System.String.Concat
        |> fun s -> s.TrimEnd('_')
       
    let private combine (job1:JobContext option) (job2:JobContext option) =
        match (job1,job2) with
        | (None,None) -> None
        | (Some(job),None) -> Some(job)
        | (None,Some(job)) -> Some(job)
        | (Some(job1'),Some(job2')) -> 
            let combinedJobs = {
                job1' with
                    Data = mergeData' job1'.Data job2'.Data
                    Path = mergePath job1'.Path job2'.Path
            }
            combinedJobs |> Some

    let inline bind (second : 'b -> Async<'c option>) (first : 'a -> Async<'b option>) : 'a -> Async<'c option> =
        fun input ->
            async {
                let! firstResult = first input
                match firstResult with
                | Some(result) -> return! second result
                | None -> return None
            }
    
    let inline (>>=) a b = bind b a

    let parallelizeWithLimit maxConcurrency (jobs:Job seq) : Job = 
        fun ctx -> ( async {
            return jobs
            |> PSeq.withDegreeOfParallelism maxConcurrency
            |> PSeq.map (fun job -> job ctx |> Async.RunSynchronously)
            |> PSeq.reduce combine
        } )

    let parallelize (jobs:Job seq) : Job =
        fun ctx -> (async {
            return
                jobs
                |> Seq.map (fun job -> job ctx)
                |> Async.Parallel |> Async.RunSynchronously
                |> PSeq.reduce combine
        }) 

    let all jobs : Job =
        fun ctx -> (async {
            return
                jobs
                |> Seq.map ((fun job -> job ctx) >> Async.RunSynchronously)
                |> Seq.reduce combine
        })

    let rec first (jobs:Job seq) : Job =
        fun ctx -> (async {
            match jobs |> Seq.toList with
            | firstJob::remainingJobs -> 
                let! jobResult = firstJob ctx
                match jobResult with
                | Some(jobResult') -> return jobResult' |> Some
                | None -> return! first remainingJobs ctx
            | [] -> return None
        })

    let withKey (key:string) : Job =
        fun ctx -> (async {
            return { ctx with Key = key |> Some } |> Some
        })
