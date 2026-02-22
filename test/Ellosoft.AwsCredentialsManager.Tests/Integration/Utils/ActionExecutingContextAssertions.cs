// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public static class ActionExecutingContextAssertionsExtensions
{
    public static ActionExecutingContextAssertions Should(this ActionExecutingContext context) => new(context);
}

public class ActionExecutingContextAssertions(ActionExecutingContext context)
{
    public ActionExecutingContextAssertions HaveValidHttpCall<TModel>(
        string expectedHttpMethod,
        string expectedPath,
        Action<TModel>? modelAssertions = null)
        where TModel : class
    {
        // TODO This is wrong it should validate a custom model instead of the context
        context.HttpContext.Request.Method.ToUpperInvariant().ShouldBe(expectedHttpMethod.ToUpperInvariant());
        context.HttpContext.Request.Path.Value.ShouldBe(expectedPath, StringComparer.OrdinalIgnoreCase);

        if (modelAssertions == null)
            return this;

        var model = context.ActionArguments.Values.OfType<TModel>().FirstOrDefault();
        model.ShouldNotBeNull();
        modelAssertions(model!);

        return this;
    }
}
