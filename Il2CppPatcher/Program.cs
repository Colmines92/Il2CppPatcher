using KeystoneNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace IL2CPP_EASY_PATCHER
{
    internal static class Program
    {
        private class ThreadedSolver
        {
            public int MaxCycleCount;
            private const int FirstNumber = 1;
            private const int SecondNumber = 1000000;

            //queues used to store the numbers to be processed
            private Queue<int>[] workQueue;
            //threads that will process the numbers
            private Thread[] workerThreads;

            //public constructor
            public ThreadedSolver()
            {
                //initialize work queue array
                workQueue = new Queue<int>[System.Environment.ProcessorCount];

                ////initialize thread array
                workerThreads = new Thread[System.Environment.ProcessorCount];

                //create a thread and a work queue for each processor core
                for (int i = 0; i < System.Environment.ProcessorCount; ++i)
                {
                    workerThreads[i] = new Thread(new ParameterizedThreadStart(this.ThreadProc));
                    workQueue[i] = new Queue<int>();
                }

                MaxCycleCount = 0;
            }

            //thread procedure
            public void ThreadProc(object obj)
            {
                int iMaxCycleCount = 1;

                //work queue index, passed as a parameter to the thread
                int iQueueIndex = (int)obj;

                //while there is data for this thread to process
                while (workQueue[iQueueIndex].Count != 0)
                {
                    long iNumberToTest = 1;
                    int iCurrentCycleCount = 1;

                    //get first number in queue
                    iNumberToTest = workQueue[iQueueIndex].Dequeue();

                    //process number
                    for (iCurrentCycleCount = 1; iNumberToTest != 1; ++iCurrentCycleCount)
                    {
                        if ((iNumberToTest & 0x1) == 0x01)
                        {
                            iNumberToTest += (iNumberToTest >> 1) + 1;
                            ++iCurrentCycleCount;
                        }
                        else
                            iNumberToTest >>= 1;
                    }

                    if (iMaxCycleCount < iCurrentCycleCount)
                        iMaxCycleCount = iCurrentCycleCount;
                }

                //when thread is done, lock to update the maximum cycle count
                lock (this)
                {
                    if (MaxCycleCount < iMaxCycleCount)
                        MaxCycleCount = iMaxCycleCount;
                }
            }

            //method used to solve the problem
            public void Solve()
            {
                //load numbers into the work queues, 250 numbers at a time
                int index = FirstNumber;
                while (index <= SecondNumber)
                {
                    for (int j = 0; j < Environment.ProcessorCount; j++)
                    {
                        int upperLimit = index + 250;
                        for (int k = index; k < upperLimit; ++k)
                            workQueue[j].Enqueue(k);

                        index += 250;
                    }
                }

                //start the threads
                for (int i = 0; i < Environment.ProcessorCount; i++)
                    workerThreads[i].Start(i);

                //wait for worker threads to finish
                for (int i = 0; i < Environment.ProcessorCount; ++i)
                    workerThreads[i].Join();
            }
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string Zip = "IL2CPP_EASY_PATCHER.resources.Ionic.Zip.Reduced.dll";
            EmbeddedAssembly.Load(Zip, "Ionic.Zip.Reduced.dll");
            string KeystoneNET = "IL2CPP_EASY_PATCHER.resources.KeystoneNET.dll";
            EmbeddedAssembly.Load(KeystoneNET, "KeystoneNET.dll");
            string Json = "IL2CPP_EASY_PATCHER.resources.Newtonsoft.Json.dll";
            EmbeddedAssembly.Load(Json, "Newtonsoft.Json.dll");

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            //stopwatch used to measure the time it takes to solve the problem
            Stopwatch tmrExecutionTime = new Stopwatch();
            //threaded solver
            ThreadedSolver ts = new ThreadedSolver();

            //solve the problem
            tmrExecutionTime.Start();
            ts.Solve();
            tmrExecutionTime.Stop();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            global.mainfrm = new MainForm();
            global.implementedLanguages.Add("en", "English");
            global.implementedLanguages.Add("es", "Español");
            global.defaultLang = Settings.LoadLanguage("en", true);
            Settings.Load();

            if (!CrateKeystone())
                MessageBox.Show(global.lang.GetStr("keystone_not_found"), global.lang.GetStr("missing_files"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                global.mainfrm.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                global.mainfrm.Text = Functions.GetTitle();

                Application.Run(global.mainfrm);
            }
        }

        static bool CrateKeystone()
        {
            if (!StartKeystone())
            {
                string path = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
                return Functions.ExtractKeystone(path);
            }
            return StartKeystone();
        }

        static bool StartKeystone()
        {
            bool result = false;
            try
            {
                global.keystone = new Keystone(KeystoneArchitecture.KS_ARCH_ARM, KeystoneMode.KS_MODE_ARM);
                result = true;
            }
            catch { }
            return result;
        }
    }
}
