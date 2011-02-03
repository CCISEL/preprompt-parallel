using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

public class StreamByteCounter {
	private const int CHUNK_SIZE = 1024;
	private volatile int pendingReads;
	private long[] counters = new long[256];
	private readonly ManualResetEvent done = new ManualResetEvent(false);
	
	public StreamByteCounter(Stream src) {	
		AsyncCallback onEndRead = null;
		onEndRead = delegate(IAsyncResult ar) {
			int c = src.EndRead(ar);
			if (c != 0) {
				lock(this) { pendingReads++; }

				//
				// For each read, we must allocate a new buffer.
				//

				byte[] nb = new byte[CHUNK_SIZE];
				src.BeginRead(nb, 0, nb.Length, onEndRead, nb);				
				
				//
				// The buffer of the current read is passed through the
				// IAsyncResult.AsyncState property.
				//
				
				byte[] cb = (byte[])ar.AsyncState;
				
				//
				// Increment occurrence counters.
				//
				
				lock(this) {
					for (int i = 0; i < c; i++) {
						counters[cb[i]]++;
					}
				}
			}
			
			//
			// The data of the current read processed; so, decrement
			// the pending counter, and if the counter reaches zero,
			// signal the "done" event.
			//
			
			lock(this) {
				if (--pendingReads == 0) {
					done.Set();
				}
			}
		};
		
		//
		// Allocate the buffer for the first read, and issue it.
		//
		
		byte[] fb = new byte[CHUNK_SIZE];
		pendingReads = 1;
		src.BeginRead(fb, 0, fb.Length, onEndRead, fb);				
	}

	public long[] GetOccurrencesPerByte {
		get {
			if (pendingReads != 0) {
				done.WaitOne();
			}
			lock(this) {
				return counters;
			}
		}
	}
 
	public bool IsCompleted {
		get { return pendingReads == 0; }
	}

	public WaitHandle AsyncWaitHandle {
		get { return done; }
	}
}
