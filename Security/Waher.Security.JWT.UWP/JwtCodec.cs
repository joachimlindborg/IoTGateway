﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Content;
using Waher.Runtime.Inventory;

namespace Waher.Security.JWT
{
	/// <summary>
	/// A JWT content coder/decoder.
	/// </summary>
	public class JwtCodec : IContentDecoder, IContentEncoder
	{
		/// <summary>
		/// application/jwt
		/// </summary>
		public const string ContentType = "application/jwt";

		/// <summary>
		/// A JWT content coder/decoder.
		/// </summary>
		public JwtCodec()
		{
		}

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes
		{
			get
			{
				return new string[] { ContentType };
			}
		}

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions
		{
			get
			{
				return new string[] { "jwt" };
			}
		}

		/// <summary>
		/// Decodes an object.
		/// </summary>
		/// <param name="ContentType">Internet Content Type.</param>
		/// <param name="Data">Encoded object.</param>
		/// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
		///	<param name="Fields">Any content-type related fields and their corresponding values.</param>
		///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
		/// <returns>Decoded object.</returns>
		/// <exception cref="ArgumentException">If the object cannot be decoded.</exception>
		public object Decode(string ContentType, byte[] Data, Encoding Encoding,
			KeyValuePair<string, string>[] Fields, Uri BaseUri)
		{
			string Token = (Encoding ?? Encoding.ASCII).GetString(Data);
			return new JwtToken(Token);
		}

		/// <summary>
		/// If the decoder decodes an object with a given content type.
		/// </summary>
		/// <param name="ContentType">Content type to decode.</param>
		/// <param name="Grade">How well the decoder decodes the object.</param>
		/// <returns>If the decoder can decode an object with the given type.</returns>
		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (ContentType == JwtCodec.ContentType)
			{
				Grade = Grade.Perfect;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			if (FileExtension.ToLower() == "jwt")
			{
				ContentType = JwtCodec.ContentType;
				return true;
			}
			else
			{
				ContentType = null;
				return false;
			}
		}

		/// <summary>
		/// Encodes an object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Encoding">Desired encoding of text. Can be null if no desired encoding is speified.</param>
		/// <param name="ContentType">Content Type of encoding. Includes information about any text encodings used.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>Encoded object.</returns>
		/// <exception cref="ArgumentException">If the object cannot be encoded.</exception>
		public byte[] Encode(object Object, Encoding Encoding, out string ContentType, params string[] AcceptedContentTypes)
		{
			if (!(Object is JwtToken Token))
				throw new ArgumentException("Object not a JWT token.", nameof(Object));

			ContentType = JwtCodec.ContentType;

			string s = Token.Header + "." + Token.Payload + "." + Token.Signature;

			if (Encoding == null)
				return Encoding.ASCII.GetBytes(s);
			else
				return Encoding.GetBytes(s);
		}

		/// <summary>
		/// If the encoder encodes a given object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Grade">How well the encoder encodes the object.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>If the encoder can encode the given object.</returns>
		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if (Object is JwtToken && InternetContent.IsAccepted(ContentType, AcceptedContentTypes))
			{
				Grade = Grade.Perfect;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}
	}
}
