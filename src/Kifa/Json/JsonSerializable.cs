namespace Kifa;

public interface JsonSerializable {
    string ToJson();
    
    // Also implement an implicit operator from string.
}
