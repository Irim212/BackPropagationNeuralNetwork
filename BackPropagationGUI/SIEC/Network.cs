
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpropagationNeuralNetwork
{
	internal class Layers
	{
		private readonly Layer inputLayer;
		private readonly Layer hiddenLayer;
		private readonly Layer outputLayer;

		internal Layers(NetworkConfiguration networkConfiguration)
		{
			inputLayer = new Layer(networkConfiguration.InputLayerNeurons);
			hiddenLayer = new Layer(networkConfiguration.HiddenLayerNeurons, inputLayer);
			outputLayer = new Layer(networkConfiguration.OutputLayerNeurons, hiddenLayer);
		}

		internal Layer getInputLayer() => inputLayer;
		internal Layer getHiddenLayer() => hiddenLayer;
		internal Layer getOutputLayer() => outputLayer;
	}

	internal class NetworkData
	{
		public List<List<double>> Inputs;
		public List<List<double>> ExpectedOutputs;
		public Dictionary<int, List<double>> ActualOutputs;

		public double OverallError;
	}

	internal struct NetworkConfiguration
	{
		public int InputLayerNeurons;
		public int HiddenLayerNeurons;
		public int OutputLayerNeurons;
		public int MaxEras;
		public double MinError;
		public double LearningRate;
		public double Momentum;
	}

	public struct SingleEraData
	{
		public double LearnProgress;
		public int CurrentEra;
		public double OverallError;
		public double PercentOfError;
	}

	public class Network
	{
		public delegate void SingleEraEndedCallback(SingleEraData singleEraData);
		public event SingleEraEndedCallback SingleEraEnded;

		private Layers layers;
		private NetworkData networkData;
		private NetworkConfiguration networkConfiguration;

		internal Network(NetworkConfiguration networkConfiguration)
		{
			initializeNetwork(networkConfiguration);
		}

		public int GetInputLayerSize()
		{
			return layers.getInputLayer().getNeurons().Count;
		}
		
		public void AddLearningPair(List<double> inputs, List<double> expectedOutputs)
		{
			if (inputs.Count != layers.getInputLayer().getNeurons().Count)
				throw new ArgumentException("Learning pair inputs count must match neurons count in input layer.");

			if (expectedOutputs.Count != layers.getOutputLayer().getNeurons().Count)
				throw new ArgumentException("Learning pair outputs count must match neurons count in output layer.");

			networkData.Inputs.Add(inputs);
			networkData.ExpectedOutputs.Add(expectedOutputs);
		}

		public void SetLearningRate(double learningRate)
		{
			networkConfiguration.LearningRate = learningRate;
		}

		public void Learn()
		{
			int era = 0;

			Random r = new Random();
			
			do
			{
				foreach(int currentInput in Enumerable.Range(0, networkData.Inputs.Count).OrderBy(x => r.Next()))
				{
					int k = 0;

					foreach (Neuron neuron in layers.getInputLayer().getNeurons())
					{
						neuron.setOutput(networkData.Inputs[currentInput][k++]);
					}

					sumWeightsAndActivate();
					calculateSignalErrors(networkData.ExpectedOutputs[currentInput]);
					updateWeights();

					List<double> currentOutputs = new List<double>();

					foreach (Neuron neuron in layers.getOutputLayer().getNeurons())
					{
						currentOutputs.Add(neuron.getOutput());
					}

					networkData.ActualOutputs[currentInput] = new List<double>(currentOutputs);
				}

				updateOverallError();

				SingleEraEnded?.Invoke(new SingleEraData()
				{
					CurrentEra = era,
					OverallError = networkData.OverallError,
					LearnProgress = (era + 1d) / networkConfiguration.MaxEras,
					PercentOfError = (networkConfiguration.MinError / networkData.OverallError * 100)
				});

				era++;
			} while (era < networkConfiguration.MaxEras && networkData.OverallError > networkConfiguration.MinError);
		}

		public List<double> GetResultForInputs(List<double> inputs)
		{
			List<double> outputs = new List<double>(networkConfiguration.OutputLayerNeurons);

			int k = 0;

			foreach(Neuron neuron in layers.getInputLayer().getNeurons())
			{
				neuron.setOutput(inputs[k++]);
			}

			sumWeightsAndActivate();

			foreach(Neuron neuron in layers.getOutputLayer().getNeurons())
			{
				outputs.Add(neuron.getOutput());
			}

			return outputs;
		}
		
		private void initializeNetwork(NetworkConfiguration networkConfiguration)
		{
			layers = new Layers(networkConfiguration);
			this.networkConfiguration = networkConfiguration;

			networkData = new NetworkData
			{
				Inputs = new List<List<double>>(),
				ExpectedOutputs = new List<List<double>>(),
				ActualOutputs = new Dictionary<int, List<double>>()
			};
		}

		private void sumWeightsAndActivate()
		{
			layers.getHiddenLayer().sumWeights();
			layers.getHiddenLayer().activate();
			layers.getOutputLayer().sumWeights();
			layers.getOutputLayer().activate();
		}

		private void calculateSignalErrors(List<double> expectedOutput)
		{
			int k = 0;

			foreach(Neuron neuron in layers.getOutputLayer().getNeurons())
			{
				double singleOutput = expectedOutput[k++];
				neuron.setSignalError((singleOutput - neuron.getOutput()) * neuron.getOutput() * (1 - neuron.getOutput()));
			}

			foreach(Neuron neuron in layers.getHiddenLayer().getNeurons())
			{
				double sum = layers.getOutputLayer().getNeurons().Select(outputNeuron => outputNeuron.getWeight(neuron) * outputNeuron.getSignalError()).Sum();

				neuron.setSignalError(neuron.getOutput() * (1 - neuron.getOutput()) * sum);
			}
		}

		private void updateWeights()
		{
			foreach(Neuron neuron in layers.getOutputLayer().getNeurons())
			{
				neuron.setBiasDiff(networkConfiguration.LearningRate * neuron.getSignalError() + networkConfiguration.Momentum * neuron.getBiasDiff());
				neuron.setBiasWeight(neuron.getBiasWeight() + neuron.getBiasDiff());

				foreach(Neuron hiddenNeuron in layers.getHiddenLayer().getNeurons())
				{
					neuron.setWeightDiff(hiddenNeuron, networkConfiguration.LearningRate * neuron.getSignalError() * hiddenNeuron.getOutput()
						+ networkConfiguration.Momentum * neuron.getWeightDiff(hiddenNeuron));

					neuron.setWeight(hiddenNeuron, neuron.getWeight(hiddenNeuron) + neuron.getWeightDiff(hiddenNeuron));
				}
			}

			foreach (Neuron neuron in layers.getHiddenLayer().getNeurons())
			{
				neuron.setBiasDiff(networkConfiguration.LearningRate * neuron.getSignalError() + networkConfiguration.Momentum * neuron.getBiasDiff());
				neuron.setBiasWeight(neuron.getBiasWeight() + neuron.getBiasDiff());

				foreach (Neuron inputNeuron in layers.getInputLayer().getNeurons())
				{
					neuron.setWeightDiff(inputNeuron, networkConfiguration.LearningRate * neuron.getSignalError() * inputNeuron.getOutput()
						+ networkConfiguration.Momentum * neuron.getWeightDiff(inputNeuron));

					neuron.setWeight(inputNeuron, neuron.getWeight(inputNeuron) + neuron.getWeightDiff(inputNeuron));
				}
			}
		}

		private void updateOverallError()
		{
			networkData.OverallError = 0;

			for(int i = 0; i < networkData.ExpectedOutputs.Count; i++)
			{
				for(int j = 0; j < networkData.ActualOutputs[i].Count; j++)
				{
					networkData.OverallError +=
						0.5d * (Math.Pow(networkData.ExpectedOutputs[i][j] - networkData.ActualOutputs[i][j], 2));
				}
			}
		}
	}
}
