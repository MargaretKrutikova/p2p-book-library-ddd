module Client.Types

open Api.Models

type UserId = Guid

type AppUser =
    | Anonymous
    | LoggedIn of UserOutputModel
