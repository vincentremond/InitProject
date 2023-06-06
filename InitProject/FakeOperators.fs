module FakeOperators

open FSharp.Quotations.Evaluator
open Fake.Core
open Fake.Core.TargetOperators
open Microsoft.FSharp.Quotations

type private UnitExpr = Expr<unit>

let private invokeExpression (expr: UnitExpr) _targetParameters = QuotationEvaluator.Evaluate expr

let private getTargetName =
    function
    | Patterns.Call(None, method, _) -> $"{method.DeclaringType.Name}: {method.Name}"
    | _ -> failwith "Expected a method call"

let private createTarget (expr: UnitExpr) =
    let targetName = getTargetName expr
    Target.create targetName (invokeExpression expr)
    targetName

let (==!>) (f1: UnitExpr) (f2: UnitExpr) = (createTarget f1) ==> (createTarget f2)

let (===>) name1 (f2: UnitExpr) = name1 ==> (createTarget f2)
