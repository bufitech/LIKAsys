using System;
using System.Threading;

namespace LIKAsys.Services
{
    public static class SingleInstance
    {
        private static Mutex _mutex;

        /// <summary>
        /// Takes the single-instance lock.
        /// <paramref name="waitSeconds"/> is used right after an update: the installer restarts
        /// LIKAsys while the old process may still be releasing the mutex, so we retry for a
        /// few seconds instead of telling the user "already running" and quitting.
        /// </summary>
        public static bool Acquire(int waitSeconds = 0)
        {
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(0, waitSeconds));
            while (true)
            {
                try
                {
                    _mutex = new Mutex(true, @"Global\LIKAsys_SingleInstance_8f2c", out bool created);
                    if (created) return true;

                    try { _mutex.Dispose(); } catch { }
                    _mutex = null;
                }
                catch { return true; }   // cannot create the mutex at all -> do not block the app

                if (DateTime.UtcNow >= deadline) return false;
                Thread.Sleep(300);
            }
        }

        public static void Release()
        {
            try { _mutex?.ReleaseMutex(); } catch { }
            try { _mutex?.Dispose(); } catch { }
            _mutex = null;
        }
    }
}
