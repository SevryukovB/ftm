using FieldTaskManager.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : ToFailureResult(result.Error, controller);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : ToFailureResult(result.Error, controller);

    public static ActionResult<T> ToFailureActionResult<T>(this Result result, ControllerBase controller) =>
        ToFailureResult(result.Error, controller);

    public static ActionResult<T> ToFailureActionResult<T>(this Result<T> result, ControllerBase controller) =>
        ToFailureResult(result.Error, controller);

    private static ObjectResult ToFailureResult(Error error, ControllerBase controller) =>
        controller.StatusCode(error.StatusCode, new { message = error.Message });
}
