// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public static class ActionExecutingContextAssertionsExtensions
{
    public static ActionExecutingContextAssertions Should(this ActionExecutingContext context) => new(context, AssertionChain.GetOrCreate());
}

public class ActionExecutingContextAssertions : ReferenceTypeAssertions<ActionExecutingContext, ActionExecutingContextAssertions>
{
    private readonly AssertionChain _chain;

    public ActionExecutingContextAssertions(ActionExecutingContext context, AssertionChain chain)
        : base(context, chain)
    {
        _chain = chain;
    }

    protected override string Identifier => "ActionExecutingContext";

    public AndConstraint<ActionExecutingContextAssertions> HaveValidHttpCall<TModel>(
        string expectedHttpMethod,
        string expectedPath,
        Action<TModel>? modelAssertions = null)
    {
        // TODO This is wrong it should validate a custom model instead of the context
        _chain
            .ForCondition(Subject.HttpContext.Request.Method.Equals(expectedHttpMethod, StringComparison.OrdinalIgnoreCase))
            .FailWith("Expected HTTP method to be {0}, but found {1}.", expectedHttpMethod, Subject.HttpContext.Request.Method);

        _chain
            .ForCondition(Subject.HttpContext.Request.Path.Value?.Equals(expectedPath, StringComparison.OrdinalIgnoreCase) == true)
            .FailWith("Expected path to be {0}, but found {1}.", expectedPath, Subject.HttpContext.Request.Path.Value);

        if (modelAssertions == null)
            return new AndConstraint<ActionExecutingContextAssertions>(this);

        var model = Subject.ActionArguments.Values.OfType<TModel>().FirstOrDefault();

        _chain
            .ForCondition(model is not null)
            .FailWith("Expected model of type {0}, but it was not found in the action arguments.", typeof(TModel).Name);

        modelAssertions(model!);

        return new AndConstraint<ActionExecutingContextAssertions>(this);
    }
}
