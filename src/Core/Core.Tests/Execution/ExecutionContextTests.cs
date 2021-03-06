﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class ExecutionContextTests
    {
        [Fact]
        public void ContextDataArePassedOn()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context => Task.CompletedTask));

            DocumentNode query = Parser.Default.Parse("{ foo }");

            var errorHandler = new Mock<IErrorHandler>();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(typeof(IErrorHandler)))
                .Returns(errorHandler.Object);

            IRequestServiceScope serviceScope =
                services.Object.CreateRequestServiceScope();

            var variables = new Mock<IVariableCollection>();

            var operation = new Mock<IOperation>();
            operation.Setup(t => t.Document).Returns(query);
            operation.Setup(t => t.Variables).Returns(variables.Object);

            var contextData = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var requestContext = new Mock<IRequestContext>();
            requestContext.Setup(t => t.ContextData).Returns(contextData);
            requestContext.Setup(t => t.ServiceScope).Returns(serviceScope);

            // act
            var executionContext = new ExecutionContext(
                schema,
                operation.Object,
                requestContext.Object,
                CancellationToken.None);

            // assert
            Assert.True(ReferenceEquals(
                contextData, executionContext.ContextData));
        }

        [Fact]
        public void CloneExecutionContext()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context => Task.CompletedTask));

            DocumentNode query = Parser.Default.Parse("{ foo }");

            var errorHandler = new Mock<IErrorHandler>();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(typeof(IErrorHandler)))
                .Returns(errorHandler.Object);

            IRequestServiceScope serviceScope = services.Object
                .CreateRequestServiceScope();

            var variables = new Mock<IVariableCollection>();

            var operation = new Mock<IOperation>();
            operation.Setup(t => t.Document).Returns(query);
            operation.Setup(t => t.Variables).Returns(variables.Object);

            var contextData = new Dictionary<string, object>
            {
                { "abc", "123" }
            };

            var diagnostics = new QueryExecutionDiagnostics(
                new DiagnosticListener("Foo"),
                new IDiagnosticObserver[0],
                TracingPreference.Never);

            var requestContext = new RequestContext(
                serviceScope,
                fs => null,
                contextData,
                diagnostics);

            // act
            var executionContext = new ExecutionContext(
                schema, operation.Object, requestContext,
                CancellationToken.None);

            IExecutionContext cloned = executionContext.Clone();

            // assert
            Assert.Equal(contextData, executionContext.ContextData);
            Assert.Equal(contextData, cloned.ContextData);
            Assert.False(object.ReferenceEquals(
                executionContext.Result, cloned.Result));
            Assert.False(object.ReferenceEquals(
                executionContext.ContextData, cloned.ContextData));
        }
    }
}
