﻿module AI

open DomainTypes

let private isTerminalNode (node: GameState) =
    node.myHand.[4].IsNone || node.opHand.[4].IsNone

let private countHandCards (hand: Hand) =
    let firstFullIndex = (hand |> Array.tryFindIndex Option.isSome)
    if firstFullIndex.IsSome then 5 - firstFullIndex.Value else 0

let evaluateNode (node: GameState) =
    let gridBalance = node.playGrid.slots |> Array.sumBy (fun slot -> 
        match slot with
            | Full c -> if c.owner = Me then 1 else -1
            | Empty _ -> 0)
    gridBalance + (countHandCards node.myHand) - (countHandCards node.opHand)


let private handWithout handIndex hand =
    let newHand = Array.copy hand
    Array.blit hand 0 newHand 1 handIndex
    newHand.[0] <- None
    newHand

let private playGridWithNewCard (playGrid: PlayGrid) (playGridIndex: int) (newCard: Card) =
    let newGridSlots = Array.copy playGrid.slots
    newGridSlots.[playGridIndex] <- Full newCard
    let updateNeighbor dirOffset thisPowerIndex =
        let neighborIndex = playGridIndex + dirOffset
        if neighborIndex >= 0 && neighborIndex <= 8 then
            let neighborSlot = playGrid.slots.[neighborIndex]
            let otherPowerIndex = 3 - thisPowerIndex
            if neighborSlot.isFull && neighborSlot.card.owner <> newCard.owner then
              if neighborSlot.card.modifiedPower otherPowerIndex < newCard.modifiedPower thisPowerIndex then
               newGridSlots.[neighborIndex] <- Full { neighborSlot.card with owner = newCard.owner }
                
    updateNeighbor -3 0 // top
    updateNeighbor -1 1 // left
    updateNeighbor +1 2 // right
    updateNeighbor +3 3 // bottom
    { slots = newGridSlots}

let private executeMove (node: GameState) isMaximizingPlayer (handIndex,playGridIndex) =
    let newTurnPhase = if isMaximizingPlayer then OpponentsTurn else MyCardSelection -1
    let newOpHand = if isMaximizingPlayer then node.opHand else handWithout handIndex node.opHand
    let newMyHand = if not isMaximizingPlayer then node.myHand else handWithout handIndex node.myHand
    let sourceHand = if isMaximizingPlayer then node.myHand else node.opHand
    let newPlayGrid = playGridWithNewCard node.playGrid playGridIndex sourceHand.[handIndex].Value
    { turnPhase = newTurnPhase; myHand = newMyHand; opHand = newOpHand; playGrid = newPlayGrid}

let childStates (node: GameState): GameState array =
    let isMaximizingPlayer = node.turnPhase <> OpponentsTurn
    let sourceHand = if isMaximizingPlayer then node.myHand else node.opHand
    let isValidMove handIndex playGridIndex =
        sourceHand.[handIndex].IsSome && node.playGrid.slots.[playGridIndex].isEmpty
    let validMoves = [| for handIndex in [0..4] do
                            for playGridIndex in [0..8] do
                                if isValidMove handIndex playGridIndex then
                                    yield (handIndex, playGridIndex) |]
    validMoves |> Array.map (executeMove node isMaximizingPlayer)

// function alphabeta(node, depth, α, β, maximizingPlayer)
//      if depth = 0 or node is a terminal node
//          return the heuristic value of node
//      if maximizingPlayer
//          v := -∞
//          for each child of node
//              v := max(v, alphabeta(child, depth - 1, α, β, FALSE))
//              α := max(α, v)
//              if β ≤ α
//                  break (* β cut-off *)
//          return v
//      else
//          v := ∞
//          for each child of node
//              v := min(v, alphabeta(child, depth - 1, α, β, TRUE))
//              β := min(β, v)
//              if β ≤ α
//                  break (* α cut-off *)
//          return v

let private alphaBeta node depth alpha beta isMaximizingPlayer =
    if depth = 0 || isTerminalNode node then
        evaluateNode node
    elif isMaximizingPlayer node then
        0
    else
        0