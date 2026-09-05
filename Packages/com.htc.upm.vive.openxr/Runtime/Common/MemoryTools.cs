using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

namespace VIVE.OpenXR
{
    internal static class MemoryTools
    {
        /// <summary>
        /// Make sure the input ptr is a OpenXR XrBaseStructure derived struct.
        /// </summary>
        /// <param name="ptr">the struct to get its next.</param>
        /// <returns>the next's value</returns>
        public static unsafe IntPtr GetNext(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return IntPtr.Zero;
            //Profiler.BeginSample("GetNext");
            XrBaseStructure* ptrToStruct = (XrBaseStructure*)ptr.ToPointer();
            //Profiler.EndSample();
            return ptrToStruct->next;
        }

        /// <summary>
        /// Make sure the input ptr is a OpenXR XrBaseStructure derived struct.
        /// </summary>
        /// <param name="ptr">the struct to get its type</param>
        /// <returns>the struct's type</returns>
        public static unsafe XrStructureType GetType(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new Exception("The input pointer is null.");

            //Profiler.BeginSample("GetType");
            XrBaseStructure* ptrToStruct = (XrBaseStructure*)ptr.ToPointer();
            //Profiler.EndSample();
            return ptrToStruct->type;
        }

        public static unsafe XrBaseStructure ToBaseStructure(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new Exception("The input pointer is null.");

            //Profiler.BeginSample("ToBaseStructure");
            XrBaseStructure* ptrToStruct = (XrBaseStructure*)ptr.ToPointer();
            //Profiler.EndSample();
            return *ptrToStruct;
        }

        public static unsafe T PtrToStructure<T>(IntPtr ptr) where T : unmanaged
        {
            //Profiler.BeginSample("PtrToStructure");
            // Not to use Marshal.PtrToStructure<T> because it is slow.
            T t = default;  // Use new T() will cause GC alloc.
            Buffer.MemoryCopy((void*)ptr, &t, sizeof(T), sizeof(T));
            //Profiler.EndSample();
            return t;
        }

        public static unsafe void PtrToStructure<T>(IntPtr ptr, ref T t) where T : unmanaged
        {
            //Profiler.BeginSample("PtrToStructure");
            fixed (T* destinationPtr = &t)
            {
                Buffer.MemoryCopy((void*)ptr, destinationPtr, sizeof(T), sizeof(T));
            }
            //Profiler.EndSample();
        }

        public static unsafe void StructureToPtr<T>(T t, IntPtr ptr) where T : unmanaged
        {
            //Profiler.BeginSample("StructureToPtr");
            // Not to use Marshal.StructureToPtr<T> because it is slow.
            Buffer.MemoryCopy(&t, (void*)ptr, sizeof(T), sizeof(T));
            //Profiler.EndSample();
        }

        /// <summary>
        /// Convert the enum array to IntPtr.  Should call <see cref="ReleaseRawMemory(IntPtr)"/> after use.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr<T>(T[] array) where T : Enum
        {
            int size = sizeof(int) * array.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);

            int* intPtr = (int*)ptr.ToPointer();
            for (int i = 0; i < array.Length; i++)
            {
                // Convert enum to int.  This has better performance than Convert.ToInt32.
                intPtr[i] = (int)(object)array[i];
            }

            return ptr;
        }

        /// <summary>
        /// Convert the struct to IntPtr.  Should call <see cref="ReleaseRawMemory(IntPtr)"/> after use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IntPtr ToIntPtr<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(structure);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, true);
            return ptr;
        }

        /// <summary>
        /// Make the same size raw buffer from input array.
        /// </summary>
        /// <typeparam name="T">Data type could be primitive type or struct. Should call <see cref="ReleaseRawMemory(IntPtr)"/> after use.</typeparam>
        /// <param name="refArray">The data array</param>
        /// <returns>The memory handle.  Should release by <see cref="ReleaseRawMemory(IntPtr)"/></returns>
        public static unsafe IntPtr MakeRawMemory<T>(T[] refArray) where T : unmanaged
        {
            int size = Marshal.SizeOf(typeof(T)) * refArray.Length;
            return Marshal.AllocHGlobal(size);
        }

        /// <summary>
        /// Copy the raw memory to the array.  You should make sure the array has the same size as the raw memory.
        /// </summary>
        /// <typeparam name="T">Convert the memory to this type array.</typeparam>
        /// <param name="array">The output array.</param>
        /// <param name="raw">The data source in raw memory form.</param>
        /// <param name="count">Specify the copy count.  Count should be less than array length.</param>
        public static unsafe void CopyFromRawMemory<T>(T[] array, IntPtr raw, int count = 0) where T : unmanaged
        {
            //Profiler.BeginSample("CopyFromRawMemory");
            int N = array.Length;
            if (count > 0 && count < array.Length)
                N = count;
            int step = sizeof(T);
            int bufferSize = step * N;

            // Pin array's address.  Prevent GC move it.
            fixed (T* destPtr = array)
            {
                T* sourcePtr = (T*)raw.ToPointer();
                Buffer.MemoryCopy(sourcePtr, destPtr, bufferSize, bufferSize);
            }
            //Profiler.EndSample();
        }

        /// <summary>
        /// Copy all raw memory to the array.  This has higher performance than <see cref="CopyFromRawMemory"/>.
        /// Use this method if you have frequent update requirements.
        /// You need prepare a byte buffer to store the raw memory.  The byte buffer size should be tSize * array.Length.
        /// tSize is used for checking the byte buffer size.  If tSize is 0, it will use Marshal.SizeOf(typeof(T)).
        /// You can save the size at your size to avoid the Marshal.Sizeof(typeof(T)) call repeatedly.
        /// </summary>
        /// <typeparam name="T">Convert the memory to this type array.</typeparam>
        /// <param name="array">The output array.</param>
        /// <param name="raw">The data source in raw memory form.</param>
        public static unsafe void CopyAllFromRawMemory<T>(T[] array, IntPtr raw) where T : unmanaged
        {
#if DEBUG
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Output array cannot be null.");
            if (raw == IntPtr.Zero)
                throw new ArgumentNullException(nameof(raw), "Raw memory pointer cannot be null.");
#endif

            //Profiler.BeginSample("CopyAllFromRawMemory");
            int elementSize = sizeof(T);
            int requiredBufferSize = elementSize * array.Length;

            // Pin array's address.  Prevent GC move it.
            fixed (T* destPtr = array)
            {
                T* sourcePtr = (T*)raw.ToPointer();
                Buffer.MemoryCopy(sourcePtr, destPtr, requiredBufferSize, requiredBufferSize);
            }
            //Profiler.EndSample();
        }

        /// <summary>
        /// Make the same size raw buffer from input array.  Make sure the raw has enough size.
        /// </summary>
        /// <typeparam name="T">Convert this type array to raw memory.</typeparam>
        /// <param name="raw">The output data in raw memory form</param>
        /// <param name="array">The data source</param>
        public static unsafe void CopyToRawMemory<T>(IntPtr raw, T[] array) where T : unmanaged
        {
            //Profiler.BeginSample("CopyToRawMemory");
            int step = sizeof(T);
            int bufferSize = step * array.Length;
            // Pin array's address.  Prevent GC move it.
            fixed (T* destPtr = array)
            {
                void* ptr = raw.ToPointer();
                Buffer.MemoryCopy(destPtr, ptr, bufferSize, bufferSize);
            }
            //Profiler.EndSample();
        }

        /// <summary>
        /// Release the raw memory handle which is created by <see cref="MakeRawMemory{T}(T[])"/>
        /// </summary>
        /// <param name="ptr"></param>
        public static void ReleaseRawMemory(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        /// <summary>
        /// Find a pointer in the next chain.  Make sure the input next pointer is a OpenXR XrBaseStructure derived struct.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="next"></param>
        /// <returns>true if exist</returns>
        public static bool HasPtrInNextChain(IntPtr target, IntPtr next)
        {
            while (next != IntPtr.Zero)
            {
                if (next == target)
                    return true;
                next = GetNext(next);
            }
            return false;
        }
    }
}
