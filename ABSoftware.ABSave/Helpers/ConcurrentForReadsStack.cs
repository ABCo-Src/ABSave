using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A thread-safe stack that allows multiple threads to iterate through (via indexers), but only
    /// allows one thread to push/pop at a time.
    /// </summary>
    internal struct ConcurrentForReadsStack<T>
    {
        public int Length;
        T[] _data;

        // Used to lock the variable when possibly going from modifying to iterating 
        // or iterating to modifying.
        SpinLock _stateChanging;

        volatile bool _currentlyModifying;
        volatile int _currentlyIterating;

        public ConcurrentForReadsStack(int capacity) => 
            (Length, _data, _stateChanging, _currentlyModifying, _currentlyIterating) = (0, new T[capacity], default, false, 0);

        public void EnterIterationLock()
        {
            bool lockAcquired = false;

            try
            {
                _stateChanging.Enter(ref lockAcquired);

                // Wait for the modifying to finish.
                var waiter = new SpinWait();
                while (_currentlyModifying) waiter.SpinOnce();

                _currentlyIterating++;
            }
            finally
            {
                if (lockAcquired) _stateChanging.Exit();
            }
        }

        public void ExitIterationLock()
        {
            Interlocked.Decrement(ref _currentlyIterating);
        }

        // This method should NOT be called from multiple threads!
        public void Push(T item)
        {
            // There's no need for us to lock when pushing, as this does not affect threads iterating.
            //LockModification();

            if (Length == _data.Length)
            {
                T[] newArr = new T[_data.Length * 2];
                Array.Copy(_data, newArr, newArr.Length);
                _data = newArr;
            }

            _data[Length++] = item;
        }

        // This method should NOT be called from multiple threads!
        public ref T Pop()
        {
            // This does need to lock, as it invalidates items threads could be looking at while iterating.
            LockModification();
            Length--;
            _currentlyModifying = false;

            return ref _data[Length];
        }

        private void LockModification()
        {
            bool lockAcquired = false;

            try
            {
                _stateChanging.Enter(ref lockAcquired);

                // While "_stateChanging" is locked, we know "_currentIterating" will only ever decrease
                // and never increase. So we just need to wait until no one is iterating anymore.
                var wait = new SpinWait();
                while (_currentlyIterating > 0) wait.SpinOnce();

                _currentlyModifying = true;
            }
            finally
            {
                if (lockAcquired) _stateChanging.Exit();
            }
        }


        void WaitForNotModifying()
        {            
        }

        public ref T this[int index] => ref _data[index];
    }
}
