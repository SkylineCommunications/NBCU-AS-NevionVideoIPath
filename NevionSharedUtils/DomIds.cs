using System;
using System.Collections.Generic;
using System.Text;

namespace DomIds
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;

	public static class Lca_Access
	{
		public const string ModuleId = "lca_access";
		public static class Enums
		{
		}

		public static class Sections
		{
			public static class StationAccessInfo
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("3af57ad9-2b22-4b79-bd28-b4a5f0a6e74a"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID Username
				{
					get;
				}

				= new FieldDescriptorID(new Guid("c08a6041-8d35-46c6-be0c-23e4d1cbc727"));
				public static FieldDescriptorID Group
				{
					get;
				}

				= new FieldDescriptorID(new Guid("2050fbf5-283e-4810-ade1-58e5c9ea317c"));
				public static FieldDescriptorID Sites
				{
					get;
				}

				= new FieldDescriptorID(new Guid("ea71c68f-7580-49a6-8cb6-4d3079dba073"));
			}

			public static class NevionControl
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("c8a19ab9-b66f-4b03-b022-7fbf88417c31"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID Profiles
				{
					get;
				}

				= new FieldDescriptorID(new Guid("f10f9ec6-d473-4cff-a917-190e042b70f5"));
			}

			public static class SwitchConfirmation
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("437b3643-3de4-4881-b88c-c51cbcf57fc3"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID ConfirmationStatus
				{
					get;
				}

				= new FieldDescriptorID(new Guid("799c931c-9834-432a-b446-c974ee269586"));
				public static FieldDescriptorID User
				{
					get;
				}

				= new FieldDescriptorID(new Guid("03f1a894-d40c-4322-a72b-8236d5b9dfc5"));
			}

			public static class BasicInformation
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("75535064-fc5a-4927-b176-af6801cdef57"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID Username
				{
					get;
				}

				= new FieldDescriptorID(new Guid("4de17669-d623-495a-bbdb-7d98f9c6fdc7"));
				public static FieldDescriptorID Group
				{
					get;
				}

				= new FieldDescriptorID(new Guid("d95096ed-82cb-4340-bc7b-50fdca15a8e3"));
			}

			public static class GangControl
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("144e8b30-f4b4-4929-a0fc-d38ae3b3e6ae"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID Username
				{
					get;
				}

				= new FieldDescriptorID(new Guid("17853335-a3d8-4cfb-b43f-0eae26a5d9ac"));
				public static FieldDescriptorID GangControlState
				{
					get;
				}

				= new FieldDescriptorID(new Guid("fb30438c-c20a-4a84-a2b4-792c73debb90"));
			}

			public static class ChannelFiltering
			{
				public static SectionDefinitionID Id
				{
					get;
				}

				= new SectionDefinitionID(new Guid("0c210141-63e6-4324-9c68-e78f53f603dd"))
				{ ModuleId = "lca_access" };
				public static FieldDescriptorID Username
				{
					get;
				}

				= new FieldDescriptorID(new Guid("18e16118-7627-432f-bc1b-f5db3ff02ecd"));
				public static FieldDescriptorID ChannelsToShow
				{
					get;
				}

				= new FieldDescriptorID(new Guid("6c7d1ad4-3845-49e7-acdf-e29e53fad8d7"));
				public static FieldDescriptorID Core
				{
					get;
				}

				= new FieldDescriptorID(new Guid("3d54e22b-f355-4b81-963a-3a73a86f8e32"));
			}
		}

		public static class Definitions
		{
			public static DomDefinitionId Session_Gang_Control
			{
				get;
			}

			= new DomDefinitionId(new Guid("55aa3f4b-d45b-420b-b98f-18ea2c2d7bef"))
			{ ModuleId = "lca_access" };
			public static DomDefinitionId Session_Channel_Filtering
			{
				get;
			}

			= new DomDefinitionId(new Guid("305ea215-db42-43ab-ab6c-876afa4a0c6d"))
			{ ModuleId = "lca_access" };
			public static DomDefinitionId Session_Switch_Confirmation
			{
				get;
			}

			= new DomDefinitionId(new Guid("78012cd9-97a3-467e-a514-e953b987cd4d"))
			{ ModuleId = "lca_access" };
			public static DomDefinitionId Nevion_Control
			{
				get;
			}

			= new DomDefinitionId(new Guid("ced88ef2-614a-44ea-9735-19b1a8e1973e"))
			{ ModuleId = "lca_access" };
			public static DomDefinitionId Station_Access
			{
				get;
			}

			= new DomDefinitionId(new Guid("15ce3453-317d-4c40-9dd6-40cb7a0fb922"))
			{ ModuleId = "lca_access" };
		}

		public static class Behaviors
		{
		}
	}
}

