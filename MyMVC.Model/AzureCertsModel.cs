namespace MyMVC.Model;

public class AzureCerts
{
    public int CertID{get; set;}
    public string Name { get; set;}
    public string Note { get; set; }

    public AzureCerts(){}
    public AzureCerts(int certID, string name, string note)
    {
        CertID = certID;
        Name = name;
        Note = note;
    }
}
