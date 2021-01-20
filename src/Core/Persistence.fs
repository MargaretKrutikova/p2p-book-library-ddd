module Core.Persistence

open System.Threading.Tasks
open Core.Domain.Types
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

type DbReadError = | MissingRecord
type DbWriteError = | WriteError

type DbResult<'a> = Task<Result<'a, DbReadError>>
type DbWriteResult = Task<Result<unit, DbWriteError>>

type UserReadModel = { Id: UserId; Name: string }

type GetUserById = UserId -> DbResult<UserReadModel>
type CreateUser = User -> DbWriteResult

type Persistence =
    { GetUserById: GetUserById
      CreateUser: CreateUser }
    

type RegisterUserArgs = { UserId: UserId; Name: string }

let registerUser (persistence: Persistence) (args: RegisterUserArgs) =
    taskResult {
        let user: User = { UserId = args.UserId; Name = args.Name }
        do! persistence.CreateUser user |> TaskResult.mapError (fun _ -> ServiceError)
        return None
    }
