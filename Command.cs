/*
 * Jesse Martinez
 * Command class with basic command info(used with ftp.cs)
 *
*/

public class Command
{

	private int paramLength;
	private string command;
	private string substituteCommand;
	private string help;

	public Command(string command, string subCMD, string help, int pLength)
	{
		this.command = command;
		this.substituteCommand = subCMD;
		this.help = help;
		this.paramLength = pLength;
	}

	////////////////////////////////////////////////////////
	///		Getters & Setters
	////////////////////////////////////////////////////////

	public int getParamLength(){return paramLength;}
	public string getCommand(){return command;}
	public string getSubstituteCommand(){return substituteCommand;}
	public string getHelp(){return help;}

	public void setParamLength(int paramLength)
	{
		this.paramLength = paramLength;
	}

	public void setCommand(string command)
	{
		this.command = command;
	}

	public void setSubstituteCommand(string substituteCommand)
	{
		this.substituteCommand = substituteCommand;
	}

	public void setHelp(string help)
	{
		this.help = help;
	}
}
