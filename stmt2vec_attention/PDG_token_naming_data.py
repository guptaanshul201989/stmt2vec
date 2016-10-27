from collections import defaultdict
import heapq
from itertools import chain, repeat
from feature_dict import FeatureDictionary
import json
import numpy as np
import scipy.sparse as sp

class TokenCodeNamingData:

    SUBTOKEN_START = "%START%"
    SUBTOKEN_END = "%END%"
    NONE = "%NONE%"

    @staticmethod
    def __get_file_data(input_file):
        
        with open(input_file, 'r') as f:
            data = json.load(f)
        names = []
        original_names = []
        code = []
        graph = []

        for entry in data:
            if not entry["tokens"]: continue
            original_names.append(",".join(entry["name"]))
            subtokens = entry["name"]
            names.append([TokenCodeNamingData.SUBTOKEN_START] + [sub.lower() for sub in subtokens] + [TokenCodeNamingData.SUBTOKEN_END])

            code_ = []
            dict = {} 
            if type(entry["tokens"][0]) != type(u"")  :
                if type(entry["tokens"][0]) == type(int(1)): entry["tokens"] = [entry["tokens"]]
                for index, sen in enumerate(entry["tokens"]):
                    sen_ = [s for s in sen if type(s) != type(int(1))]
                    code_.append(TokenCodeNamingData.remove_identifiers_markers([token.lower() for token in sen_]))
                    dict[int(sen[0])] = index
            else:
                code_.append(TokenCodeNamingData.remove_identifiers_markers([token.lower() for token in entry["tokens"]]))
            code.append(code_)

            graph_ = np.zeros((3 * (len(code_)), len(code_)))
            for i in range(len(code_)):
		        graph_[i, i] = 1
		        graph_[i + len(code_), i] = 1
		        graph_[i + 2 * len(code_), i] = 1
            try:
                for link in entry["cfedges"]:  
		            graph_[dict[int(link.split("->")[0])] + graph_.shape[1], dict[int(link.split("->")[1])]] = 1
                for link in entry["cfedges"]:  
		            graph_[dict[int(link.split("->")[1])] + graph_.shape[1], dict[int(link.split("->")[0])]] = 1

                for link in entry["ddedges"]:
		            graph_[dict[int(link.split("->")[0])] + 2 * graph_.shape[1], dict[int(link.split("->")[1])]] = 1
                for link in entry["ddedges"]:
		            graph_[dict[int(link.split("->")[1])] + 2 * graph_.shape[1], dict[int(link.split("->")[0])]] = 1
            except:
                pass
            graph.append(graph_)
        return names, code, graph, original_names
    def code2codeMatix(self, code):
        code_matrix = []
        max_len = max([len(item) for item in code])
        for sen in code:
            code_matrix.extend([item for item in sen] + [self.all_tokens_dictionary.get_id_or_unk(self.NONE) for ii in range(max_len - len(sen))])
        return code_matrix
    def code2sens(self, codes):
        code_sens = []
        for code in codes:
            for sen in code:
                code_sens.append([item for item in sen])
        return code_sens
    def __init__(self, names, code):
        self.name_dictionary = FeatureDictionary.get_feature_dictionary_for(chain.from_iterable(names), 2)
        self.name_dictionary.add_or_get_id(self.NONE)

        self.all_tokens_dictionary = FeatureDictionary.get_feature_dictionary_for(chain.from_iterable(
            [chain.from_iterable(self.code2sens(code)), chain.from_iterable(names)]), 5)
        self.all_tokens_dictionary.add_or_get_id(self.NONE)
        self.name_empirical_dist = self.__get_empirical_distribution(self.all_tokens_dictionary, chain.from_iterable(names))

    @staticmethod
    def __get_empirical_distribution(element_dict, elements, dirichlet_alpha=10.):
        """
        Retrive te empirical distribution of tokens
        :param element_dict: a dictionary that can convert the elements to their respective ids.
        :param elements: an iterable of all the elements
        :return:
        """
        targets = np.array([element_dict.get_id_or_unk(t) for t in elements])
        empirical_distribution = np.bincount(targets, minlength=len(element_dict)).astype(float)
        empirical_distribution += dirichlet_alpha / len(empirical_distribution)
        return empirical_distribution / (np.sum(empirical_distribution) + dirichlet_alpha)


    @staticmethod
    def keep_identifiers_only(self, code):
        filtered_code = []
        for tokens in code:
            identifier_tokens = []
            in_id = False
            for t in tokens:
                if t == "<id>":
                    in_id = True
                elif t == '</id>':
                    in_id = False
                elif in_id:
                    identifier_tokens.append(t)
            filtered_code.append(identifier_tokens)
        return filtered_code

        
    @staticmethod
    def remove_identifiers_markers(code):
        return filter(lambda t: t != "<id>" and t != "</id>", code)


    def get_data_in_recurrent_copy_convolution_format(self, input_file, min_code_size):
        names, code,graph, original_names = self.__get_file_data(input_file)
        return self.get_data_for_recurrent_copy_convolution(names, code,graph, min_code_size), original_names


    def get_data_for_recurrent_copy_convolution(self, names, code, graph, sentence_padding):
        assert len(names) == len(code), (len(names), len(code), code.shape)
        name_targets = []
        target_is_unk = []
        copy_vectors = []
        code_sentences = []
        code_graph = []
        padding = [self.all_tokens_dictionary.get_id_or_unk(self.NONE)]

        for i, name in enumerate(names):
            code_sens = []
            for iii, sen in enumerate(code[i]):
                code_sentence = [self.all_tokens_dictionary.get_id_or_unk(t) for t in sen]
                code_sens.append(np.array(code_sentence, dtype=np.int32))
            if sentence_padding % 2 == 0:
                code_sentence = padding * (sentence_padding / 2)  + padding * (sentence_padding / 2)
            else:
                code_sentence = padding * (sentence_padding / 2 + 1)  + padding * (sentence_padding / 2)

            name_tokens = [self.all_tokens_dictionary.get_id_or_unk(t) for t in name]
            unk_tokens = [self.all_tokens_dictionary.is_unk(t) for t in name]
            target_can_be_copied = [[t == subtok for t in self.code2codeMatix(code[i])] for subtok in name]
            name_targets.append(np.array(name_tokens, dtype=np.int32))
            target_is_unk.append(np.array(unk_tokens, dtype=np.int32))
            copy_vectors.append(np.array(target_can_be_copied, dtype=np.int32))
            code_sentences.append(np.array(code_sens, dtype=np.object).T)

            code_graph.append(np.array(graph[i], dtype=np.int32))

        name_targets = np.array(name_targets, dtype=np.object)
        code_sentences = np.array(code_sentences, dtype=np.object)
        code_graph = np.array(code_graph)
        #print code_graph.shape
        code = np.array(code, dtype=np.object)
        target_is_unk = np.array(target_is_unk, dtype=np.object)
        copy_vectors = np.array(copy_vectors, dtype=np.object)
        return name_targets, code_sentences, code_graph, code, target_is_unk, copy_vectors
 

    @staticmethod
    def get_data_in_recurrent_copy_convolution_format_with_validation(input_file, pct_train, min_code_size):
        assert pct_train < 1
        assert pct_train > 0
        names, code, graph, original_names = TokenCodeNamingData.__get_file_data(input_file)

        names = np.array(names, dtype=np.object)
        code = np.array(code, dtype=np.object)
        graph = np.array(graph, dtype=np.object)
        lim = int(pct_train * len(names))
        idxs = np.arange(len(names))
        np.random.shuffle(idxs)
        naming = TokenCodeNamingData(names[idxs[:lim]], code[idxs[:lim]])
        return naming.get_data_for_recurrent_copy_convolution(names[idxs[:lim]], code[idxs[:lim]],graph[idxs[:lim]], min_code_size),\
                naming.get_data_for_recurrent_copy_convolution(names[idxs[lim:]], code[idxs[lim:]],graph[idxs[lim:]], min_code_size), naming


    def get_suggestions_given_name_prefix(self, next_name_log_probs, name_cx_size, max_predicted_identifier_size=5, max_steps=100):
        suggestions = defaultdict(lambda: float('-inf'))  # A list of tuple of full suggestions (token, prob)
        # A stack of partial suggestion in the form ([subword1, subword2, ...], logprob)
        possible_suggestions_stack = [
            ([self.NONE] * (name_cx_size - 1) + [self.SUBTOKEN_START], [], 0)]
        # Keep the max_size_to_keep suggestion scores (sorted in the heap). Prune further exploration if something has already
        # lower score
        predictions_probs_heap = [float('-inf')]
        max_size_to_keep = 15
        nsteps = 0
        while True:
            scored_list = []
            while len(possible_suggestions_stack) > 0:
                subword_tokens = possible_suggestions_stack.pop()
                
                # If we're done, append to full suggestions
                if subword_tokens[0][-1] == self.SUBTOKEN_END:
                    final_prediction = tuple(subword_tokens[1][:-1])
                    if len(final_prediction) == 0:
                        continue
                    log_prob_of_suggestion = np.logaddexp(suggestions[final_prediction], subword_tokens[2])
                    if log_prob_of_suggestion > predictions_probs_heap[0] and not log_prob_of_suggestion == float('-inf'):
                        # Push only if the score is better than the current minimum and > 0 and remove extraneous entries
                        suggestions[final_prediction] = log_prob_of_suggestion
                        heapq.heappush(predictions_probs_heap, log_prob_of_suggestion)
                        if len(predictions_probs_heap) > max_size_to_keep:
                            heapq.heappop(predictions_probs_heap)
                    continue
                elif len(subword_tokens[1]) > max_predicted_identifier_size:  # Stop recursion here
                    continue
    
                # Convert subword context
                context = [self.name_dictionary.get_id_or_unk(k) for k in
                           subword_tokens[0][-name_cx_size:]]
                assert len(context) == name_cx_size
                context = np.array([context], dtype=np.int32)
    
                # Predict next subwords
                target_subword_logprobs = next_name_log_probs(context)
    
                def get_possible_options(name_id):
                    subword_name = self.all_tokens_dictionary.get_name_for_id(name_id)
                    if subword_name == self.all_tokens_dictionary.get_unk():
                        subword_name = "***"
                    name = subword_tokens[1] + [subword_name]
                    return subword_tokens[0][1:] + [subword_name], name, target_subword_logprobs[0, name_id] + \
                           subword_tokens[2]
                
                top_indices = np.argsort(-target_subword_logprobs[0])
                possible_options = [get_possible_options(top_indices[i]) for i in xrange(max_size_to_keep)]
                # Disallow suggestions that contain duplicated subtokens.
                scored_list.extend(filter(lambda x: len(x[1])==1 or x[1][-1] != x[1][-2], possible_options))

                # Prune
            scored_list = filter(lambda suggestion: suggestion[2] >= predictions_probs_heap[0] and suggestion[2] >= float('-inf'), scored_list)
            scored_list.sort(key=lambda entry: entry[2], reverse=True)

                # Update
            possible_suggestions_stack = scored_list[:max_size_to_keep]
            nsteps += 1
            if nsteps >= max_steps:
                break

        # Sort and append to predictions
        suggestions = [(identifier, np.exp(logprob)) for identifier, logprob in suggestions.items()]
        suggestions.sort(key=lambda entry: entry[1], reverse=True)
        return suggestions

