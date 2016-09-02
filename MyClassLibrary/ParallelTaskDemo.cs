
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace MyClassLibrary
{
    public class ParallelTaskDemo
    {
        public static void Run()
        {
            int[] data = new int[100];
            int i = 0;

            for (i = 0; i < data.Length; i++)
            {
                data[i] = i;
                Console.Write("{0} ", data[i]);
            }
            Console.WriteLine(" \n ");

            Console.WriteLine("\nParallel running ... ... \n");
            Parallel.For(0, data.Length, (j, state) =>
            {

                Console.Write("{0} ", data[j]);
                System.Threading.Thread.Sleep(2000);
                if (j > 4)
                {
                    state.Break();
                }
            });
            Console.WriteLine("\n\nParallel end ... ... \n");

            var task = Task.Factory.StartNew(() =>
            {

                Thread.Sleep(3000);
                Console.WriteLine("wake up.");
            });
            Console.WriteLine("Begin...");
            //Task.WaitAll(task);
            Console.WriteLine("Done.");



            Console.ReadKey();
        }

        public static void Run2()
        {
            StopLoop();
            BreakAtThreshold();


        }

        public static void Run3()
        {

            // Source must be array or IList.
            var source = Enumerable.Range(0, 100).ToArray();

            // Partition the entire source array.
            var rangePartitioner = Partitioner.Create(0, source.Length);

            double[] results = new double[source.Length];

            // Loop over the partitions in parallel.
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                // Loop over each range element without a delegate invocation.
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    Console.WriteLine(range.Item1 + "," + range.Item2);
                    results[i] = source[i] * Math.PI;
                }
            });

            Console.WriteLine("Operation complete. Print results? y/n");
            char input = Console.ReadKey().KeyChar;
            if (input == 'y' || input == 'Y')
            {
                foreach (double d in results)
                {
                    Console.Write("{0} ", d);
                }
            }
        }

        public static void Run4()
        {
            int[] nums = Enumerable.Range(0, 10).ToArray();
            long total = 0;
            List<long> initList = new List<long>();
            List<long> tempList = new List<long>();
            List<long> finalList = new List<long>();
            // Use type parameter to make subtotal a long, not an int
            Parallel.For<long>(0, nums.Length, () => 0, (j, loop, subtotal) =>
            {
                subtotal += nums[j];
                return subtotal;
            },
                (x) => Interlocked.Add(ref total, x)
            );

            Console.WriteLine("The total is {0}", total);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }


        private static void StopLoop()
        {
            Console.WriteLine("Stop loop...");
            double[] source = MakeDemoSource(1000, 1);
            ConcurrentStack<double> results = new ConcurrentStack<double>();

            // i is the iteration variable. loopState is a 
            // compiler-generated ParallelLoopState
            Parallel.For(0, source.Length, (i, loopState) =>
            {
                // Take the first 100 values that are retrieved
                // from anywhere in the source.
                Console.Write("index: " + i + ", ");
                if (i < 100)
                {
                    // Accessing shared object on each iteration
                    // is not efficient. See remarks.
                    double d = Compute(source[i]);
                    results.Push(d);
                }
                else
                {
                    loopState.Stop();
                    return;
                }

            } // Close lambda expression.
            ); // Close Parallel.For

            Console.WriteLine("Results contains {0} elements", results.Count());
        }


        static void BreakAtThreshold()
        {
            double[] source = MakeDemoSource(10000, 1.0002);
            ConcurrentStack<double> results = new ConcurrentStack<double>();

            // Store all values below a specified threshold.
            Parallel.For(0, source.Length, (i, loopState) =>
            {
                double d = Compute(source[i]);
                results.Push(d);
                if (d > .2)
                {
                    // Might be called more than once!
                    loopState.Break();
                    Console.WriteLine("Break called at iteration {0}. d = {1} ", i, d);
                    Thread.Sleep(1000);
                }
            });

            Console.WriteLine("results contains {0} elements", results.Count());
        }

        static double Compute(double d)
        {
            //Make the processor work just a little bit.
            return Math.Sqrt(d);
        }


        // Create a contrived array of monotonically increasing
        // values for demonstration purposes. 
        static double[] MakeDemoSource(int size, double valToFind)
        {
            double[] result = new double[size];
            double initialval = .01;
            for (int i = 0; i < size; i++)
            {
                initialval *= valToFind;
                result[i] = initialval;
            }

            return result;
        }

    }
}


