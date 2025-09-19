using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Exceptions;
using Shouldly;
using Xunit;

namespace Shared.Tests;

public class ExceptionHelperTests {
    [Fact]
    public void ExceptionHelper_GetCombinedExceptionMessages_MessagesCombined() {
        var exception = new Exception("message1", new Exception("message2"));

        var message = exception.GetCombinedExceptionMessages();

        message.ShouldBe($"message1{Environment.NewLine}message2{Environment.NewLine}");

    }

    [Fact]
    public void ExceptionHelper_GetExceptionMessages_MessagesCombined()
    {
        var exception = new Exception("message1", new Exception("message2"));

        var messages = exception.GetExceptionMessages();

        messages.Count.ShouldBe(2);
        messages[0].ShouldBe("message1");
        messages[1].ShouldBe("message2");
    }

    [Fact]
    public void ExceptionHelper_GetCombinedExceptionMessages_ExceptionNull_NomMessage() {
        Exception exception = null;

        var message = ExceptionHelper.GetCombinedExceptionMessages(exception);

        message.ShouldBeEmpty();
    }

    [Fact]
    public void ExceptionHelper_GetExceptionMessages_ExceptionNull_NomMessage() {
        Exception exception = null;

        var messages = ExceptionHelper.GetExceptionMessages(exception);

        messages.ShouldBeEmpty();

    }
}