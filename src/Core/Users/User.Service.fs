module Core.Users.Service

open Core.Common.Persistence
open Core.Common.SimpleTypes

open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling
open System.Threading.Tasks

type UserError =
  | UserWithProvidedNameNotFound
  | ServiceError // TODO: create common type for errors

type UserReadResult<'a> = Task<Result<'a, UserError>>
type UserCommandResult = Task<Result<unit, UserError>>

type CreateUserDto = {
  UserId: UserId
  Name: string
}

module private Conversions = 
  let toDomainError (domainError: UserError) (error: Queries.DbReadError): UserError =
    match error with
    | Queries.MissingRecord -> domainError

module CreateUser =
  type Composed = CreateUserDto -> UserCommandResult
  type Service =  Commands.CreateUser -> Composed

  let execute: Service =
    fun createUser dto -> 
      taskResult {
        let userModel: Commands.UserCreateModel = {
          UserId = dto.UserId
          Name = dto.Name
        }
        do! createUser userModel |> TaskResult.mapError (fun _ -> ServiceError)
      }

module GetUserByName =
  type Composed = string -> UserReadResult<Queries.UserReadModel>
  type Service = Queries.GetUserByName -> Composed

  let run: Service =
    fun getUserByName userName -> 
      taskResult {
        let! existingUser = 
            getUserByName userName 
            |> TaskResult.mapError (Conversions.toDomainError UserWithProvidedNameNotFound)

        return existingUser
      }
