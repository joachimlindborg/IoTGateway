﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Networking.HTTP
{
	/// <summary>
	/// HTTP resource defined by a GET delegate method.
	/// </summary>
	public class HttpGetDelegateResource : HttpResource, IHttpGetMethod
	{
		private HttpAuthenticationScheme[] authenticationSchemes;
		private HttpMethodHandler get;
		private bool synchronous;
		private bool handlesSubPaths;
		private bool userSessions;

		/// <summary>
		/// HTTP resource defined by a GET delegate method.
		/// </summary>
		/// <param name="ResourceName">Name of resource.</param>
		/// <param name="GET">GET Method.</param>
		/// <param name="Synchronous">If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).</param>
		/// <param name="HandlesSubPaths">If sub-paths are handled.</param>
		/// <param name="UserSessions">If the resource uses user sessions.</param>
		/// <param name="AuthenticationSchemes">Any authentication schemes used to authenticate users before access is granted.</param>
		public HttpGetDelegateResource(string ResourceName, HttpMethodHandler GET, bool Synchronous, bool HandlesSubPaths,
			bool UserSessions, params HttpAuthenticationScheme[] AuthenticationSchemes)
			: base(ResourceName)
		{
			this.get = GET;
			this.synchronous = Synchronous;
			this.handlesSubPaths = HandlesSubPaths;
			this.authenticationSchemes = AuthenticationSchemes;
			this.userSessions = UserSessions;
		}

		/// <summary>
		/// If the resource is synchronous (i.e. returns a response in the method handler), or if it is asynchronous
		/// (i.e. sends the response from another thread).
		/// </summary>
		public override bool Synchronous
		{
			get { return this.synchronous; }
		}

		/// <summary>
		/// If the resource handles sub-paths.
		/// </summary>
		public override bool HandlesSubPaths
		{
			get { return this.handlesSubPaths; }
		}

		/// <summary>
		/// If the resource uses user sessions.
		/// </summary>
		public override bool UserSessions
		{
			get { return this.userSessions; }
		}

		/// <summary>
		/// If the GET method is allowed.
		/// </summary>
		public bool AllowsGET
		{
			get
			{
				return this.get != null;
			}
		}

		/// <summary>
		/// Any authentication schemes used to authenticate users before access is granted to the corresponding resource.
		/// </summary>
		/// <param name="Request">Current request</param>
		public override HttpAuthenticationScheme[] GetAuthenticationSchemes(HttpRequest Request)
		{
			return this.authenticationSchemes;
		}

		/// <summary>
		/// Executes the GET method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public void GET(HttpRequest Request, HttpResponse Response)
		{
			if (this.get == null)
				throw new MethodNotAllowedException(this.AllowedMethods);
			else
				this.get(Request, Response);
		}

	}
}
