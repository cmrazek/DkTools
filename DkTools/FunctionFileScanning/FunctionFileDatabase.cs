﻿////------------------------------------------------------------------------------
//// <auto-generated>
////     This code was generated by a tool.
////     Runtime Version:4.0.30319.34014
////
////     Changes to this file may cause incorrect behavior and will be lost if
////     the code is regenerated.
//// </auto-generated>
////------------------------------------------------------------------------------

//// 
//// This source code was auto-generated by xsd, Version=4.0.30319.17929.
//// 
//namespace DkTools.FunctionFileScanning.FunctionFileDatabase {
//	using System.Xml.Serialization;
    
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	[System.Xml.Serialization.XmlRootAttribute("database", Namespace="http://ProbeTools/FunctionFileDatabase.xsd", IsNullable=false)]
//	public partial class Database_t {
        
//		private Application_t[] applicationField;
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("application")]
//		public Application_t[] application {
//			get {
//				return this.applicationField;
//			}
//			set {
//				this.applicationField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class Application_t {
        
//		private string nameField;
        
//		private FunctionFile_t[] fileField;
        
//		private Function_t[] functionField;
        
//		private Class_t[] classField;
        
//		/// <remarks/>
//		public string name {
//			get {
//				return this.nameField;
//			}
//			set {
//				this.nameField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("file")]
//		public FunctionFile_t[] file {
//			get {
//				return this.fileField;
//			}
//			set {
//				this.fileField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("function")]
//		public Function_t[] function {
//			get {
//				return this.functionField;
//			}
//			set {
//				this.functionField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("class")]
//		public Class_t[] @class {
//			get {
//				return this.classField;
//			}
//			set {
//				this.classField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class FunctionFile_t {
        
//		private string fileNameField;
        
//		private System.DateTime modifiedField;
        
//		/// <remarks/>
//		public string fileName {
//			get {
//				return this.fileNameField;
//			}
//			set {
//				this.fileNameField = value;
//			}
//		}
        
//		/// <remarks/>
//		public System.DateTime modified {
//			get {
//				return this.modifiedField;
//			}
//			set {
//				this.modifiedField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class Class_t {
        
//		private string nameField;
        
//		private string fileNameField;
        
//		private Function_t[] functionField;
        
//		/// <remarks/>
//		public string name {
//			get {
//				return this.nameField;
//			}
//			set {
//				this.nameField = value;
//			}
//		}
        
//		/// <remarks/>
//		public string fileName {
//			get {
//				return this.fileNameField;
//			}
//			set {
//				this.fileNameField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("function")]
//		public Function_t[] function {
//			get {
//				return this.functionField;
//			}
//			set {
//				this.functionField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class Function_t {
        
//		private string nameField;
        
//		private string signatureField;
        
//		private string fileNameField;
        
//		private Span_t spanField;
        
//		private DataType_t dataTypeField;
        
//		private FunctionPrivacy_t privacyField;
        
//		private bool privacyFieldSpecified;
        
//		/// <remarks/>
//		public string name {
//			get {
//				return this.nameField;
//			}
//			set {
//				this.nameField = value;
//			}
//		}
        
//		/// <remarks/>
//		public string signature {
//			get {
//				return this.signatureField;
//			}
//			set {
//				this.signatureField = value;
//			}
//		}
        
//		/// <remarks/>
//		public string fileName {
//			get {
//				return this.fileNameField;
//			}
//			set {
//				this.fileNameField = value;
//			}
//		}
        
//		/// <remarks/>
//		public Span_t span {
//			get {
//				return this.spanField;
//			}
//			set {
//				this.spanField = value;
//			}
//		}
        
//		/// <remarks/>
//		public DataType_t dataType {
//			get {
//				return this.dataTypeField;
//			}
//			set {
//				this.dataTypeField = value;
//			}
//		}
        
//		/// <remarks/>
//		public FunctionPrivacy_t privacy {
//			get {
//				return this.privacyField;
//			}
//			set {
//				this.privacyField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlIgnoreAttribute()]
//		public bool privacySpecified {
//			get {
//				return this.privacyFieldSpecified;
//			}
//			set {
//				this.privacyFieldSpecified = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class Span_t {
        
//		private Position_t startField;
        
//		private Position_t endField;
        
//		/// <remarks/>
//		public Position_t start {
//			get {
//				return this.startField;
//			}
//			set {
//				this.startField = value;
//			}
//		}
        
//		/// <remarks/>
//		public Position_t end {
//			get {
//				return this.endField;
//			}
//			set {
//				this.endField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class Position_t {
        
//		private int offsetField;
        
//		private int lineNumField;
        
//		private int linePosField;
        
//		/// <remarks/>
//		public int offset {
//			get {
//				return this.offsetField;
//			}
//			set {
//				this.offsetField = value;
//			}
//		}
        
//		/// <remarks/>
//		public int lineNum {
//			get {
//				return this.lineNumField;
//			}
//			set {
//				this.lineNumField = value;
//			}
//		}
        
//		/// <remarks/>
//		public int linePos {
//			get {
//				return this.linePosField;
//			}
//			set {
//				this.linePosField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Diagnostics.DebuggerStepThroughAttribute()]
//	[System.ComponentModel.DesignerCategoryAttribute("code")]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public partial class DataType_t {
        
//		private string nameField;
        
//		private string[] completionOptionField;
        
//		/// <remarks/>
//		public string name {
//			get {
//				return this.nameField;
//			}
//			set {
//				this.nameField = value;
//			}
//		}
        
//		/// <remarks/>
//		[System.Xml.Serialization.XmlElementAttribute("completionOption")]
//		public string[] completionOption {
//			get {
//				return this.completionOptionField;
//			}
//			set {
//				this.completionOptionField = value;
//			}
//		}
//	}
    
//	/// <remarks/>
//	[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
//	[System.SerializableAttribute()]
//	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ProbeTools/FunctionFileDatabase.xsd")]
//	public enum FunctionPrivacy_t {
        
//		/// <remarks/>
//		Public,
        
//		/// <remarks/>
//		Private,
        
//		/// <remarks/>
//		Protected,
//	}
//}
