/****************************************************************************************
 * Author : DauAu
 * Created: 12-Mar-2025
 * Purpose: Represents a Todotype entity with ID, Typename, and Note fields.
 * Modification History:
 * Date         Author           Description
 * -----------  ---------------  ------------------------------------------------------
 * 12-Mar-2025  DauAu            Created class
 ****************************************************************************************/

public class TodoType
{
    public int ID { get; set; }
    public string Typename { get; set; }
    public string Note { get; set; }

    public TodoType() { }

    public TodoType(int id, string typename, string note)
    {
        ID = id;
        Typename = typename;
        Note = note;
    }
}
