using System;

namespace TagLib.TrueAudio
{
	/// <summary>
	/// This struct implements <see cref="IAudioCodec" /> to provide
	/// support for reading TrueAudio audio properties
	/// </summary>
	public struct StreamHeader : IAudioCodec
	{
		#region Private Fields

		private readonly int version;
		private readonly int bitrate;
		private readonly int sampleRate;
		private readonly int bitsPerSample;
		private readonly ulong sampleFrames;
		private readonly double samplesPerFrame;
		private readonly int channels;
		private readonly ulong length;
		private readonly long streamLength;

		private const double frameTime = 1.04489795918367346939;

		#endregion

		#region Public Static Fields

		/// <summary>
		/// Size of a TrueAudio header.
		/// </summary>
		public const int Size = 18;

		/// <summary>
		/// Identifier used to identify a TrueAudio file.
		/// </summary>
		public static readonly ReadOnlyByteVector FileIdentifier = "TTA";



		#endregion

		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StreamHeader" /> for a specified header block and
		///    stream length.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the stream
		///    header data.
		/// </param>
		/// <param name="streamLength">
		///    A <see cref="long" /> value containing the length of the
		///    TrueAudio stream in bytes.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> does not begin with <see
		///    cref="FileIdentifier" /> or is less than <see cref="Size"
		///    /> bytes long.
		/// </exception>
		public StreamHeader(ByteVector data, long streamLength)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			if (!data.StartsWith(FileIdentifier))
				throw new CorruptFileException("Data does not begin with identifier");

			if (data.Count < Size)
				throw new CorruptFileException("Insufficient data in stream header");

			this.streamLength = streamLength;

			int pos = 3;

			version = (int)char.GetNumericValue((char)data[pos]);
			pos += 1;

			// According to http://en.true-audio.com/TTA_Lossless_Audio_Codec_-_Format_Description
			// TTA2 headers are in development, and have a different format
			if (version == 1)
			{
				// Skip audio format.
				pos += 2;

				channels = data.ToUShort(pos, false);
				pos += 2;

				bitsPerSample = data.ToUShort(pos, false);
				pos += 2;

				sampleRate = (int) data.ToUInt(pos, false);
				pos += 4;

				sampleFrames = data.ToUInt(pos, false);
				samplesPerFrame = (frameTime*sampleRate);
				length = ((sampleRate > 0) ? (sampleFrames/(ulong)sampleRate) : 0);

				bitrate = (int) ((length > 0) ? (((ulong)streamLength*8)/length)/1000 : 0);
			}
			else // Version 2
			{
				channels = data.ToUShort(pos, false);
				pos += 2;

				bitsPerSample = data.ToUShort(pos, false);
				pos += 2;

				sampleRate = (int) data.ToUInt(pos, false);
				pos += 4;

				// NOTE: Here as a placeholder.
				//       If this is needed, make it into a getter.
				// Flags
				//
				//  - 0x1    - Front Left
				//  - 0x2    - Front Right
				//  - 0x4    - Front Center
				//  - 0x8    - Primary LFE (LFE Left)
				//  - 0x10   - Back Left (Left Surround)
				//  - 0x20   - Back Right (Right Surround)
				//  - 0x40   - Left (Left Wide)
				//  - 0x80   - Right (Right Wide)
				//  - 0x100  - Back Center (Back Surround)
				//  - 0x200  - Side Left (Left Surround Diffuse)
				//  - 0x400  - Side Right (Right Surround Diffuse)
				//  - 0x800  - Top Center (Center Height)
				//  - 0x1000 - Top Left (Left Height)
				//  - 0x2000 - Top Right (Right Height)
				//  - 0x4000 - Secondary LFE (LFE Right)
				// uint channelMask = data.ToUInt(pos, false);
				pos += 4;

				sampleFrames = data.ToULong(pos, false);
				samplesPerFrame = (frameTime * sampleRate);
				pos += 8;

				length = data.ToULong(pos, false);
				bitrate = (int)((length > 0) ? (((ulong)streamLength * 8) / length) / 1000 : 0);
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		public TimeSpan Duration
		{
			get
			{
				if (sampleRate <= 0 && streamLength <= 0)
					return TimeSpan.Zero;

				// TODO: Is this correct?
				return TimeSpan.FromSeconds((sampleFrames*samplesPerFrame)/sampleRate);
			}
		}

		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagLib.MediaTypes.Audio" />.
		/// </value>
		public MediaTypes MediaTypes
		{
			get { return MediaTypes.Audio; }
		}

		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public string Description
		{
			get { return "TrueAudio file"; }
		}

		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing a bitrate of the
		///    audio represented by the current instance.
		/// </value>
		public int AudioBitrate
		{
			get { return bitrate; }
		}

		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample rate of
		///    the audio represented by the current instance.
		/// </value>
		public int AudioSampleRate
		{
			get { return sampleRate; }
		}

		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		public int AudioChannels
		{
			get { return channels; }
		}

		/// <summary>
		/// Gets the number of bits per sample.
		/// </summary>
		/// <value>
		/// An <see cref="int"/> value representing
		/// the number of bits per sample.
		/// </value>
		public int BitsPerSample
		{
			get { return bitsPerSample; }
		}

		/// <summary>
		/// Gets the number of sample frames in this TrueAudio file.
		/// </summary>
		/// <value>
		/// An <see cref="ulong"/> representing the number
		/// of sample frames within this TrueAudio file.
		/// </value>
		public ulong SampleFrames
		{
			get { return sampleFrames; }
		}

		/// <summary>
		/// Gets the number of samples per audio frame.
		/// </summary>
		/// <value>
		/// An <see cref="double"/> value containing the number of
		/// audio samples are within each frame in this TrueAudio file.
		/// </value>
		public double SamplesPerFrame
		{
			get { return samplesPerFrame; }
		}

		/// <summary>
		///    Gets the TrueAudio version of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the TrueAudio version
		///    of the audio represented by the current instance.
		/// </value>
		public int Version
		{
			get { return version; }
		}

		#endregion
	}
}
