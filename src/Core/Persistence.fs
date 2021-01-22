module Core.Persistence

open System.Threading.Tasks
open Core.Domain.Types

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
    