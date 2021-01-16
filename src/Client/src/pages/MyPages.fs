module Client.Pages.MyPages

open Api.Models
open Client.Types
open Client.Utils
open Client.Api

open System
open Core.QueryModels
open Feliz
open Elmish
open Feliz.UseElmish
open Fable.React.Props
open Fable.React
open Fulma


let view dispatch model =
    Tabs.tabs [ Tabs.Size IsLarge ]
        [ Tabs.tab [ Tabs.Tab.IsActive true ]
            [ a [ ]
                [ str "My published books" ] ]
          Tabs.tab [ ]
            [ a [ ]
                [ str "My activity" ] ]
        ]