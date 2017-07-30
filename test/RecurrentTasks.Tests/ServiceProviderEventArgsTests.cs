namespace RecurrentTasks
{
    using System;
    using Xunit;

    public class ServiceProviderEventArgsTests
    {
        [Fact]
        public void ThrowExceptionOnNullException()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => new ServiceProviderEventArgs(null));
        }
    }
}
