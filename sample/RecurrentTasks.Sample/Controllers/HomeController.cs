namespace RecurrentTasks.Sample.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        private ITask myTask;

        public HomeController(ITask<SampleTask> myTask)
        {
            this.myTask = myTask;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string command)
        {
            switch (command)
            {
                case "STOP":
                    if (myTask.IsStarted)
                    {
                        myTask.Stop();
                    }

                    break;
                case "START":
                    if (!myTask.IsStarted)
                    {
                        myTask.Start(TimeSpan.Zero);
                    }

                    break;
                case "TRYRUN":
                    if (myTask.IsStarted)
                    {
                        myTask.TryRunImmediately();
                    }

                    break;
            }

            return RedirectToAction("Index");
        }
    }
}
