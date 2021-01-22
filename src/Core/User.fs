module Core.User

open Core.Domain.Errors
open Core.Domain.Types
open Core.Persistence
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

type RegisterUserArgs = { UserId: UserId; Name: string }

let registerUser (persistence: Persistence) (args: RegisterUserArgs) =
    taskResult {
        let user: User = { UserId = args.UserId; Name = args.Name }
        do! persistence.CreateUser user |> TaskResult.mapError (fun _ -> AppError.ServiceError)
        return None
    }
