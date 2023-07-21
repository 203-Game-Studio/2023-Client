// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Login.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from Login.proto</summary>
public static partial class LoginReflection {

  #region Descriptor
  /// <summary>File descriptor for Login.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static LoginReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "CgtMb2dpbi5wcm90byIoCgxDU19Mb2dpbl9SZXESCwoDdWlkGAEgASgFEgsK",
          "A3B3ZBgCIAEoCSIuCgxTQ19Mb2dpbl9SZXMSDAoEY29kZRgBIAEoCRIQCghk",
          "ZXZpY2VJZBgCIAEoCSIhCg1DU19Mb2dvdXRfUmVxEhAKCGRldmljZUlkGAEg",
          "ASgJIh0KDVNDX0xvZ291dF9SZXMSDAoEY29kZRgBIAEoCWIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::CS_Login_Req), global::CS_Login_Req.Parser, new[]{ "Uid", "Pwd" }, null, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::SC_Login_Res), global::SC_Login_Res.Parser, new[]{ "Code", "DeviceId" }, null, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::CS_Logout_Req), global::CS_Logout_Req.Parser, new[]{ "DeviceId" }, null, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::SC_Logout_Res), global::SC_Logout_Res.Parser, new[]{ "Code" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class CS_Login_Req : pb::IMessage<CS_Login_Req> {
  private static readonly pb::MessageParser<CS_Login_Req> _parser = new pb::MessageParser<CS_Login_Req>(() => new CS_Login_Req());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<CS_Login_Req> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::LoginReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Login_Req() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Login_Req(CS_Login_Req other) : this() {
    uid_ = other.uid_;
    pwd_ = other.pwd_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Login_Req Clone() {
    return new CS_Login_Req(this);
  }

  /// <summary>Field number for the "uid" field.</summary>
  public const int UidFieldNumber = 1;
  private int uid_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int Uid {
    get { return uid_; }
    set {
      uid_ = value;
    }
  }

  /// <summary>Field number for the "pwd" field.</summary>
  public const int PwdFieldNumber = 2;
  private string pwd_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string Pwd {
    get { return pwd_; }
    set {
      pwd_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as CS_Login_Req);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(CS_Login_Req other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (Uid != other.Uid) return false;
    if (Pwd != other.Pwd) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (Uid != 0) hash ^= Uid.GetHashCode();
    if (Pwd.Length != 0) hash ^= Pwd.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (Uid != 0) {
      output.WriteRawTag(8);
      output.WriteInt32(Uid);
    }
    if (Pwd.Length != 0) {
      output.WriteRawTag(18);
      output.WriteString(Pwd);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (Uid != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(Uid);
    }
    if (Pwd.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(Pwd);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(CS_Login_Req other) {
    if (other == null) {
      return;
    }
    if (other.Uid != 0) {
      Uid = other.Uid;
    }
    if (other.Pwd.Length != 0) {
      Pwd = other.Pwd;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 8: {
          Uid = input.ReadInt32();
          break;
        }
        case 18: {
          Pwd = input.ReadString();
          break;
        }
      }
    }
  }

}

public sealed partial class SC_Login_Res : pb::IMessage<SC_Login_Res> {
  private static readonly pb::MessageParser<SC_Login_Res> _parser = new pb::MessageParser<SC_Login_Res>(() => new SC_Login_Res());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<SC_Login_Res> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::LoginReflection.Descriptor.MessageTypes[1]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Login_Res() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Login_Res(SC_Login_Res other) : this() {
    code_ = other.code_;
    deviceId_ = other.deviceId_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Login_Res Clone() {
    return new SC_Login_Res(this);
  }

  /// <summary>Field number for the "code" field.</summary>
  public const int CodeFieldNumber = 1;
  private string code_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string Code {
    get { return code_; }
    set {
      code_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "deviceId" field.</summary>
  public const int DeviceIdFieldNumber = 2;
  private string deviceId_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string DeviceId {
    get { return deviceId_; }
    set {
      deviceId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as SC_Login_Res);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(SC_Login_Res other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (Code != other.Code) return false;
    if (DeviceId != other.DeviceId) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (Code.Length != 0) hash ^= Code.GetHashCode();
    if (DeviceId.Length != 0) hash ^= DeviceId.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (Code.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(Code);
    }
    if (DeviceId.Length != 0) {
      output.WriteRawTag(18);
      output.WriteString(DeviceId);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (Code.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(Code);
    }
    if (DeviceId.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceId);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(SC_Login_Res other) {
    if (other == null) {
      return;
    }
    if (other.Code.Length != 0) {
      Code = other.Code;
    }
    if (other.DeviceId.Length != 0) {
      DeviceId = other.DeviceId;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          Code = input.ReadString();
          break;
        }
        case 18: {
          DeviceId = input.ReadString();
          break;
        }
      }
    }
  }

}

public sealed partial class CS_Logout_Req : pb::IMessage<CS_Logout_Req> {
  private static readonly pb::MessageParser<CS_Logout_Req> _parser = new pb::MessageParser<CS_Logout_Req>(() => new CS_Logout_Req());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<CS_Logout_Req> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::LoginReflection.Descriptor.MessageTypes[2]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Logout_Req() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Logout_Req(CS_Logout_Req other) : this() {
    deviceId_ = other.deviceId_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public CS_Logout_Req Clone() {
    return new CS_Logout_Req(this);
  }

  /// <summary>Field number for the "deviceId" field.</summary>
  public const int DeviceIdFieldNumber = 1;
  private string deviceId_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string DeviceId {
    get { return deviceId_; }
    set {
      deviceId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as CS_Logout_Req);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(CS_Logout_Req other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (DeviceId != other.DeviceId) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (DeviceId.Length != 0) hash ^= DeviceId.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (DeviceId.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(DeviceId);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (DeviceId.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceId);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(CS_Logout_Req other) {
    if (other == null) {
      return;
    }
    if (other.DeviceId.Length != 0) {
      DeviceId = other.DeviceId;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          DeviceId = input.ReadString();
          break;
        }
      }
    }
  }

}

public sealed partial class SC_Logout_Res : pb::IMessage<SC_Logout_Res> {
  private static readonly pb::MessageParser<SC_Logout_Res> _parser = new pb::MessageParser<SC_Logout_Res>(() => new SC_Logout_Res());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<SC_Logout_Res> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::LoginReflection.Descriptor.MessageTypes[3]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Logout_Res() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Logout_Res(SC_Logout_Res other) : this() {
    code_ = other.code_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public SC_Logout_Res Clone() {
    return new SC_Logout_Res(this);
  }

  /// <summary>Field number for the "code" field.</summary>
  public const int CodeFieldNumber = 1;
  private string code_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string Code {
    get { return code_; }
    set {
      code_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as SC_Logout_Res);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(SC_Logout_Res other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (Code != other.Code) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (Code.Length != 0) hash ^= Code.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (Code.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(Code);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (Code.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(Code);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(SC_Logout_Res other) {
    if (other == null) {
      return;
    }
    if (other.Code.Length != 0) {
      Code = other.Code;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          Code = input.ReadString();
          break;
        }
      }
    }
  }

}

#endregion


#endregion Designer generated code
