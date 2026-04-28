using System.ComponentModel;
using System.Data.Common;

namespace SO;


public class AlgoritmoBanqueiro
{
    private const int NUMBER_OF_CUSTOMERS = 5;
    private const int NUMBER_OF_RESOURCES = 3;

    private int[] available = new int[NUMBER_OF_RESOURCES];
    public int[] Available { get => available; set => available = value; }
    private int[,] maximum = new int[NUMBER_OF_CUSTOMERS, NUMBER_OF_RESOURCES];
    public int[,] Maximum { get => maximum; set => maximum = value; }
    private int[,] allocation = new int[NUMBER_OF_CUSTOMERS, NUMBER_OF_RESOURCES];
    public int[,] Allocation { get => allocation; set => allocation = value; }
    private int[,] need = new int[NUMBER_OF_CUSTOMERS, NUMBER_OF_RESOURCES];
    public int[,] Need { get => need; set => need = value; }
    public readonly object _lockBanco = new object();

    public AlgoritmoBanqueiro()
    {
        //SALDO DO BANCO
        this.available = [30,20,20];
        //-
        int[,] ints =
        {
            //VALORES DAS CONTAS DOS USUÁRIOS
            {7,4,2}, //0
            {5,6,1}, //1
            {4,4,4}, //2
            {7,0,2}, //3
            {6,4,9}  //4
            //-
        };
        this.maximum = ints;
        this.need = (int[,])ints.Clone();
    }

    //-
    public bool EhEstadoSeguro()
    {
        // 1.cópias para não corromper os dados reais durante a simulação
        int[] work = (int[])available.Clone();
        bool[] finish = new bool[NUMBER_OF_CUSTOMERS]; // Todos começam como false

        // 2. Loop para tentar "finalizar" todos os clientes
        for (int count = 0; count < NUMBER_OF_CUSTOMERS; count++)
        {
            bool encontrouAlgum = false;

            for (int i = 0; i < NUMBER_OF_CUSTOMERS; i++)
            {
                // Se o cliente ainda não "terminou" na simulação
                if (!finish[i])
                {
                    bool podeAtender = true;
                    
                    for (int j = 0; j < NUMBER_OF_RESOURCES; j++)
                    {
                        if (need[i, j] > work[j])
                        {
                            podeAtender = false;
                            break;
                        }
                    }

                    if (podeAtender)
                    {
                        for (int j = 0; j < NUMBER_OF_RESOURCES; j++)
                        {
                            work[j] += allocation[i, j];
                        }
                        finish[i] = true;
                        encontrouAlgum = true;
                    }
                }
            }

            // Se passamos por todos os clientes e nenhum pôde ser atendido, paramos.
            if (!encontrouAlgum) break;
        }

        // 3. O estado é seguro se, e somente se, TODOS os clientes conseguiram "terminar"
        foreach (bool f in finish)
        {
            if (!f) return false; // Se um sequer ficou false, o estado é inseguro!
        }

        return true;
    }
}
class Program
{
    static void Main(string[] args)
    {
        AlgoritmoBanqueiro banco = new AlgoritmoBanqueiro();
        
        
        //-
        //REQUEST   
        int request_resources(int costumer_num, int[] request)
        {
            lock (banco._lockBanco)
            {
                //O request é mais do que o Need?
                for(int i  = 0; i<3; i++)
                {
                    if (request[i] > banco.Need[costumer_num, i])
                    {
                        System.Console.WriteLine("ERRO: Cliente "+costumer_num+"  pediu mais do que o máxmo permitido.");
                        return -1;
                    }
                }
                //O banco tem os recursos?
                for(int i  = 0; i<3; i++)
                {
                    if (request[i] > banco.Available[i])
                    {
                        System.Console.WriteLine("ERRO: Recursos insuficientes para o cliente "+costumer_num);
                        return -1;
                    }
                }

                System.Console.WriteLine("Enviando recursos...");

                //Simulando...
                for(int i  = 0; i<3; i++)
                {
                    banco.Available[i]-= request[i];
                    banco.Allocation[costumer_num,i]+=request[i];
                    banco.Need[costumer_num,i] -= request[i];
                }

                //Segurança
                if (banco.EhEstadoSeguro())
                {
                    System.Console.WriteLine("SUCESSO, o cliente "+costumer_num+" alocou recursos com segurança.");
                    return 0;
                }
                else
                {
                    //ROLLBACK
                    for(int i  = 0; i<3; i++)
                    {
                    banco.Available[i]+= request[i];
                    banco.Allocation[costumer_num,i]-=request[i];
                    banco.Need[costumer_num,i] += request[i];
                    }
                    System.Console.WriteLine("ACESSO NEGADO: Pedido do cliente "+costumer_num+" causaria deadlock");
                    return -1;
                }
            }
        }
        //-
        int release_resources(int costumer_num, int[] retirada)
        {
            lock (banco._lockBanco)
            {
                System.Console.WriteLine("[DEVOLUÇÃO] O cliente "+costumer_num+" está devolvendo recursos...");
                
                //Ao tentar devolver mais do que tem.
                for(int i  = 0; i<3; i++)
                {
                    if (retirada[i] > banco.Allocation[costumer_num, i])
                    {
                        System.Console.WriteLine("ERRO: tentativa de devolver mais do que tem.");
                        return -1;
                    }
                //Atualizando
                banco.Available[i] += retirada[i];
                banco.Allocation[costumer_num,i] -= retirada[i];
                
                //Need aumenta
                banco.Need[costumer_num,i] += retirada[i];

                }

                System.Console.WriteLine("[INFO] Recursos devolvidos.");

                return 0;
            }
        }
        //-
        void LogicaDoCliente(int id)
        {
            Random random = new Random();
            while (true)
            {
                int[] solicitacao = GerarSolicitaçãoAleatória(id);
                if(request_resources(id, solicitacao) == 0)
                {
                    Thread.Sleep(random.Next(1000, 3000));

                    release_resources(id, solicitacao);

                    Thread.Sleep(random.Next(100, 500));
                }
                else
                {
                    Thread.Sleep(2000);
                }
            }
        }
        //-
        //Solicitação Aleatória
        //-
        int[] GerarSolicitaçãoAleatória(int id)
        {
            Random random = new Random();
            int[] ints = new int[3];
            lock (banco._lockBanco)
            {
                for(int i  = 0; i<3; i++)
                {
                    int necessidade = banco.Need[id, i];
    
                    //Se precisar, geta com o Next...
                    ints[i] = necessidade > 0 ? random.Next(0, necessidade + 1) : 0;
                }
                return ints;
            }
        }
        //-
        //Criando as threads
        for(int i = 0; i< 5; i++)
        {
            Thread t = new Thread(() => LogicaDoCliente(i));

            t.Start();

            System.Console.WriteLine($"> Thread do cliente {i} inicializada;");
        }
    }
}
