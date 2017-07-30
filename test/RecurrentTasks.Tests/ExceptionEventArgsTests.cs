namespace RecurrentTasks
{
    using System;
    using Moq;
    using Xunit;

    public class ExceptionEventArgsTests
    {
        [Fact]
        public void ThrowExceptionOnNullException()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();

            Assert.ThrowsAny<ArgumentNullException>(() => new ExceptionEventArgs(serviceProviderMock.Object, null));
        }
    }
}
