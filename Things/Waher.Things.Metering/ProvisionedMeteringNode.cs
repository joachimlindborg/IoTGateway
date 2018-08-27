﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using Waher.Networking.XMPP.DataForms.Layout;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Runtime.Settings;
using Waher.Things.Attributes;
using Waher.Things.DisplayableParameters;

namespace Waher.Things.Metering
{
	/// <summary>
	/// Base class for all provisioned metering nodes.
	/// </summary>
	public abstract class ProvisionedMeteringNode : MetaMeteringNode, IPropertyFormAnnotation
	{
		private string owner = string.Empty;
		private bool provisioned = false;
		private bool isPublic = false;

		/// <summary>
		/// Base class for all provisioned metering nodes.
		/// </summary>
		public ProvisionedMeteringNode()
			: base()
		{
		}

		/// <summary>
		/// If the node is provisioned is not. Property is editable.
		/// </summary>
		[Page(18, "Provisioning", 50)]
		[Header(19, "Provision node.")]
		[ToolTip(20, "If checked, the node will be registered in the Thing Registry (if available), and access rights will be controlled by the corresponding owner (if a Provisioning Server is available).")]
		[DefaultValue(false)]
		public bool Provisioned
		{
			get { return this.provisioned; }
			set
			{
				if (this.provisioned && !value && !string.IsNullOrEmpty(this.owner))
					throw new Exception("Device is owned by " + this.owner + ". Device must be disowned first.");

				this.provisioned = value;
			}
		}

		/// <summary>
		/// If the node is provisioned is not. Property is editable.
		/// </summary>
		[Page(18, "Provisioning", 50)]
		[Header(21, "Owner:")]
		[ToolTip(22, "Communication address of owner.")]
		[DefaultValueStringEmpty]
		[ReadOnly]
		public string OwnerAddress
		{
			get { return this.owner; }
			set
			{
				if (this.provisioned && !string.IsNullOrEmpty(this.owner) && this.owner != value)
					throw new Exception("Device is owned by " + this.owner + ". Device must be disowned first.");

				this.owner = value;
			}
		}

		/// <summary>
		/// If the node is public in the regitry or not.
		/// </summary>
		[Page(18, "Provisioning", 50)]
		[Header(23, "Public node.")]
		[ToolTip(24, "If the node is registered as a public node in the Thing Registry.")]
		[DefaultValue(false)]
		[ReadOnly]
		public bool Public
		{
			get { return this.isPublic; }
			set
			{
				if (this.provisioned && !value && this.isPublic && !string.IsNullOrEmpty(this.owner))
					throw new Exception("Device is owned by " + this.owner + ". Device must be disowned first.");

				this.isPublic = value;
			}
		}

		/// <summary>
		/// If node can be provisioned.
		/// </summary>
		public override bool IsProvisioned => this.provisioned;

		/// <summary>
		/// Who the owner of the node is. The empty string means the node has no owner.
		/// </summary>
		public override string Owner => this.owner;

		/// <summary>
		/// If the node is public.
		/// </summary>
		public override bool IsPublic => this.isPublic;

		/// <summary>
		/// Gets displayable parameters.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <param name="Caller">Information about caller.</param>
		/// <returns>Set of displayable parameters.</returns>
		public override async Task<IEnumerable<Parameter>> GetDisplayableParametersAsync(Language Language, RequestOrigin Caller)
		{
			LinkedList<Parameter> Result = await base.GetDisplayableParametersAsync(Language, Caller) as LinkedList<Parameter>;

			Result.AddLast(new BooleanParameter("Provisioned", await Language.GetStringAsync(typeof(MeteringTopology), 25, "Provisioned"), this.provisioned));

			if (this.provisioned)
			{
				Result.AddLast(new StringParameter("Owner", await Language.GetStringAsync(typeof(MeteringTopology), 26, "Owner"), this.owner));
				Result.AddLast(new BooleanParameter("Public", await Language.GetStringAsync(typeof(MeteringTopology), 27, "Public"), this.isPublic));
			}

			return Result;
		}

		/// <summary>
		/// Called when node has been claimed by an owner.
		/// </summary>
		/// <param name="Owner">Owner</param>
		/// <param name="IsPublic">If node is public.</param>
		public override Task Claimed(string Owner, bool IsPublic)
		{
			this.owner = Owner;
			this.isPublic = IsPublic;

			if (this.ObjectId != Guid.Empty)
				return this.NodeUpdated();
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Called when node has been disowned by its owner.
		/// </summary>
		public override Task Disowned()
		{
			this.owner = string.Empty;
			this.isPublic = false;

			if (this.ObjectId != Guid.Empty)
				return this.NodeUpdated();
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Called when node has been removed from the registry.
		/// </summary>
		public override Task Removed()
		{
			this.isPublic = false;

			if (this.ObjectId != Guid.Empty)
				return this.NodeUpdated();
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Annotates the property form.
		/// </summary>
		/// <param name="Form">Form being built.</param>
		public virtual async Task AnnotatePropertyForm(FormState Form)
		{
			if (this.provisioned && string.IsNullOrEmpty(this.owner))
			{
				string Uri = await RuntimeSettings.GetAsync("IoTDisco.KEY." + this.NodeId + "." + this.SourceId + "." + this.Partition, string.Empty);
				if (!string.IsNullOrEmpty(Uri))
				{
					Language Language = await Translator.GetLanguageAsync(Form.LanguageCode);
					Namespace Namespace = await Language.GetNamespaceAsync(typeof(MeteringTopology).Namespace);
					Field Field;

					Field = new TextMultiField(Form.Form, "IoTDiscoUri", await Namespace.GetStringAsync(94, "URI to claim node:"), false,
						new string[] { Uri }, null, await Namespace.GetStringAsync(95, "The owner can use this URI to claim ownership of the node."),
						null, null, null, false, true, false)
					{
						Priority = HeaderAttribute.DefaultPriority,
						Ordinal = Form.FieldOrdinal++
					};

					Form.Fields.Add(Field);

					if (Form.PageByLabel.TryGetValue(await Namespace.GetStringAsync(18, "Provisioning"), out Page Page))
						Page.Add(new FieldReference(Form.Form, Field.Var));
				}
			}
		}


	}
}
