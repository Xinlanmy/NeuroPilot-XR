using AOT;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace VIVE.OpenXR.Common.RenderThread
{
	#region syncObject
	public class Message
	{
		public bool isFree = true;
	}

	/// <summary>
	/// MessagePool class manages a pool of message objects for reuse.  You can enter any kind of message object.
	/// However when obtain, the message object will not able to cast to the type you want.
	/// You should only use one kind of message.  Not mix different kind of message.
	/// </summary>
	public class MessagePool
	{
		// pool member is used to store message objects in a list.
		// Note that the size of this list will dynamically adjust as needed but will not automatically shrink.
		private readonly List<Message> pool = new List<Message>(2) { };
		private int index = 0;

		public MessagePool() { }

		// Next method calculates the next index value for cycling through message objects in the pool.
		private int Next(int value)
		{
			if (++value >= pool.Count)
				value = 0;
			return value;
		}

		// Obtain method retrieves a message object from the pool.
		// Ensure proper state setup for the message after retrieval and call Release() to the message after use.
		public T Obtain<T>() where T : Message, new()
		{
			int c = pool.Count;
			int i = index;
			for (int j = 0; j < c; i++, j++)
			{
				if (i >= c)
					i = 0;
				if (pool[i].isFree)
				{
					//Debug.LogError("Obtain idx=" + i);
					index = i;
					return (T)pool[i];
				}
			}
			index = Next(i);
			var newItem = new T()
			{
				isFree = true
			};
			pool.Insert(index, newItem);
			//Log.d("RT.MessagePool.Obtain<" + typeof(T) + ">()  pool count=" + pool.Count);  // Not to expose developer's type.
			Log.D("RT.MessagePool.Obtain()  pool count=" + pool.Count);
			return newItem;
		}

		// Lock method marks a message as "in use" to prevent other code from reusing it.
		// This is already called to the message obtained from the pool.
		public static void Lock(Message msg)
		{
			msg.isFree = false;
		}

		/// <summary>
		/// Release method marks a message as "free" so that other code can reuse it.
		/// You can use it in RenderThread.  It will not trigger the GC event.
		/// </summary>
		/// <param name="msg"></param>
		public static void Release(Message msg)
		{
			msg.isFree = true;
		}
	}

	/// <summary>
	/// PreAllocatedQueue class is a message queue based on MessagePool for preallocating message objects.
	/// Its main functionality is to add message objects to the queue and retrieve them from the queue.
	/// Messages should be enqueued in GameThread and dequeued in RenderThread.
	/// In render thread, dequeue will not trigger the GC event.  Because the queue is preallocated.
	/// The 'lock' expression is not used for list's size change.  Because lock should be avoid used in RenderThread.
	/// Set the queueSize as the double count of message you want to pass to render thread in one frame, and the
	/// list will never change size during runtime.  Therefore we don't need to use 'lock' to protect the list.
	/// </summary>
	public class PreAllocatedQueue : MessagePool
	{
		// list member is used to store preallocated message objects in a list.
		// Note that the size of this list is set during initialization and does not dynamically adjust.
		private List<Message> list = new List<Message>();
		private int queueBegin = 0;
		private int queueEnd = 0;

		/// <summary>
		/// The queueSize should be the double count of message you want to pass to render thread in one frame.
		/// </summary>
		/// <param name="queueSize"></param>
		public PreAllocatedQueue(int queueSize = 2) : base()
		{
			for (int i = 0; i < queueSize; i++)
			{
				list.Add(null);
			}
		}

		private int Next(int value)
		{
			if (++value >= list.Count)
				value = 0;
			return value;
		}

		/// <summary>
		/// Enqueue method adds a message object to the queue.
		/// If the queue is full, the new message is added to the end of the list.
		/// 
		/// This function is designed to use the message object obtained from the MessagePool.
		/// Ensure only one type of message object is used in the queue.
		/// 
		/// Enqueue will increase the queue size if the queue is full.  This may trigger GC.Alloc.
		/// This function should be used in GameThread.
		/// </summary>
		/// <param name="msg"></param>
		public void Enqueue(Message msg)
		{
			Lock(msg);
			queueEnd = Next(queueEnd);

			// If the queue is full, add the message to the end of the list.  Should not let it happen.
			// Use larger queue size to avoid this issue.
			// If you see the error log here, you should increase the queue size in your design.
			if (queueEnd == queueBegin)
			{
				// Should let Insert and queueBegin be atomic.  No lock protection here.
				list.Insert(queueEnd, msg);
				queueBegin++;
				Debug.LogError("RT.MessagePool.Enqueue()  list count=" + list.Count);
			}
			else
			{
				list[queueEnd] = msg;
			}
		}

		/// <summary>
		/// Dequeue method retrieves a message object from the queue.
		/// This method returns the first message object in the queue and removes it from the queue.
		/// This function will not trigger the GC event.  Free to use in RenderThread.
		/// After use the Message, call Release() to the message.
		/// </summary>
		/// <returns></returns>
		public Message Dequeue()
		{
			// No lock protection here.  If list is not change size, it is safe.
			// However if list changed size, it is safe in most case.
			queueBegin = Next(queueBegin);
			return list[queueBegin];
		}
	}

	/// <summary>
	/// RenderThreadTask class is used to execute specified tasks on the rendering thread.
	/// You don't need to develop a native function to run your task on the rendering thread.
	/// And you don't need to design how to pass data to render thread.
	/// This class can be run in Unity Editor since Unity 2021.  Test your code in Unity Editor can save your time.
	///
	/// You should only create RenderThreadTask as static readonly.  Do not create RenderThreadTask in dynamic.
	/// 
	/// You should not run Unity.Engine code in RenderThread.  It will cause the Unity.Engine to hang.
	/// Any exception will not be caught and shown in RenderThread.
	/// You should print your error message out to clearify your issue.
	/// 
	/// The 'lock' expression is not used here.  Because I believe the lock is not necessary in this case.
	/// And the lock will cause the performance issue.  All the design here help you not to use 'lock'.
	/// </summary>
	public class RenderThreadTask
	{
		private static IntPtr GetFunctionPointerForDelegate(Delegate del)
		{
			return System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(del);
		}

		public delegate void RenderEventDelegate(int e);
		private static readonly RenderEventDelegate handle = new RenderEventDelegate(RunSyncObjectInRenderThread);
		private static readonly IntPtr handlePtr = GetFunctionPointerForDelegate(handle);

		public delegate void Receiver(PreAllocatedQueue dataQueue);

		// CommandList is used to store all RenderThreadTask objects.
		// Do not create RenderThreadTask object in dynamic.  It will cause the CommandList to increase infinitly.
		private static List<RenderThreadTask> CommandList = new List<RenderThreadTask>();

		private PreAllocatedQueue queue;
		public PreAllocatedQueue Queue { get { return queue; } }

		private readonly Receiver receiver;
		private readonly int id;

		/// <summary>
		/// Input the receiver as render thread callback.  The receiver will be executed in render thread.
		/// queueSize should be the double count of message you want to pass to render thread in one frame.
		/// </summary>
		/// <param name="render">The callback in render thread.</param>
		/// <param name="queueSize">If issue this event once in a frame, set queueSize as 2.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public RenderThreadTask(Receiver render, int queueSize = 2)
		{
			queue = new PreAllocatedQueue(queueSize);
			receiver = render;
			if (receiver == null)
				throw new ArgumentNullException("receiver should not be null");

			CommandList.Add(this);
			id = CommandList.IndexOf(this);
		}

		~RenderThreadTask()
		{
			// Remove could be in a random order, and will cause orderId change. DO not remove any of them.
			//try { CommandList.Remove(this); } finally { }
		}

		void IssuePluginEvent(IntPtr callback, int eventID)
		{
			// Older version will hang after run script in render thread.
			GL.IssuePluginEvent(callback, eventID);
			return;
		}

		void IssuePluginEvent(CommandBuffer cmdBuf, IntPtr callback, int eventID)
		{
			cmdBuf.IssuePluginEvent(callback, eventID);
			return;
		}

		/// <summary>
		/// IssueEvent method submits this task's receiver, which is set in constructor, to be executed on the rendering thread.
		/// </summary>
		public void IssueEvent()
		{
			// Let the render thread run the RunSyncObjectInRenderThread(id)
			IssuePluginEvent(handlePtr, id);
		}

		public void IssueInCommandBuffer(CommandBuffer cmdBuf)
		{
			// Let the render thread run the RunSyncObjectInRenderThread(id)
			IssuePluginEvent(cmdBuf, handlePtr, id);
		}

		// Called by RunSyncObjectInRenderThread()
		private void Receive()
		{
			receiver(queue);
		}

		// RunSyncObjectInRenderThread method is a static method used to execute a specified task on the rendering thread.
		// This method is invoked by Unity's rendering event mechanism and does not need to be called directly by developers.
		[MonoPInvokeCallback(typeof(RenderEventDelegate))]
		private static void RunSyncObjectInRenderThread(int id)
		{
			CommandList[id].Receive();
		}
	}
	#endregion

	#region sample
	// Not to compile this sample into your application.  Just for reference.  You can run this sample in Unity Editor and it will work.
#if UNITY_EDITOR
	public class ViveRenderThreadTaskSample : MonoBehaviour
	{
		// Create your own message class.
		internal class SampleMessage : Message
		{
			public int dataPassedToRenderThread;
		}

		// Use static readonly to create RenderThreadTask.  Keep internal to avoid miss use by other developers.
		internal static readonly RenderThreadTask sampleRenderThreadTask1 = new RenderThreadTask(SampleReceiver1);
		// Different task use different RenderThreadTask and different recevier.
		internal static readonly RenderThreadTask sampleRenderThreadTask2 = new RenderThreadTask(SampleReceiver2);

		private static void SampleReceiver1(PreAllocatedQueue dataQueue)
		{
			var msg = dataQueue.Dequeue() as SampleMessage;
			if (msg != null)
			{
				// Keep data before release.  Use local variable to keep data and release msg early.  Should not keep the msg instance itself.
				var data = msg.dataPassedToRenderThread;
				// Make sure release the msg if finished.  Other wise the memory will keep increasing when Obtain.
				MessagePool.Release(msg);
				Debug.Log("Task1, the data passed to render thread: " + data);
			}
		}

		private static void SampleReceiver2(PreAllocatedQueue dataQueue)
		{
			var msg = dataQueue.Dequeue() as SampleMessage;
			if (msg != null)
			{
				// Keep data before release.  Use local variable to keep data and release msg early.  Should not keep the msg instance itself.
				var data = msg.dataPassedToRenderThread;
				// Make sure release the msg if finished.  Other wise the memory will keep increasing when Obtain.
				MessagePool.Release(msg);
				Debug.Log("Task2, the data passed to render thread: " + data);
			}
		}

		// Send a message to the render thread every frame.
		private void Update()
		{
			// Make sure only one kind of message object is used in the queue.
			var msg = sampleRenderThreadTask1.Queue.Obtain<SampleMessage>();
			msg.dataPassedToRenderThread = 123;
			sampleRenderThreadTask1.Queue.Enqueue(msg);
			sampleRenderThreadTask1.IssueEvent();
		}

		// Send a message to render thread when something clicked.  Make sure only one click in one frame because the queue size is only two.
		public void OnClicked()
		{
			// Reuse the same message type is ok.
			var msg = sampleRenderThreadTask2.Queue.Obtain<SampleMessage>();
			msg.dataPassedToRenderThread = 234;
			sampleRenderThreadTask2.Queue.Enqueue(msg);
			sampleRenderThreadTask2.IssueEvent();
		}
	}
#endif
	#endregion
}
